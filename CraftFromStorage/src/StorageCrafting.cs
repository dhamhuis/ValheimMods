using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CraftFromStorage;

internal static class StorageCrafting
{
    private const float OwnershipTimeout = 5f;

    private static readonly Dictionary<Container, Request> _requests = new();
    private static readonly HashSet<Container> _responseGuards = new();
    private static readonly Dictionary<Container, ManualOpen> _manualOpens = new();
    private static readonly List<Container> _reserved = new();
    private static bool _cancelled;

    internal static bool IsPending { get; private set; }

    internal static bool IsExecuting { get; private set; }

    internal static bool HasRequest(Container container) => _requests.ContainsKey(container);

    internal static bool IsBusy(Container container)
    {
        if (_manualOpens.TryGetValue(container, out var manual) && Time.realtimeSinceStartup >= manual.Deadline)
        {
            _manualOpens.Remove(container);
        }

        return HasRequest(container) || _reserved.Contains(container) || _manualOpens.ContainsKey(container);
    }

    internal static void PrepareManualOpen(Container container, Humanoid character, bool hold)
    {
        var player = Player.m_localPlayer;

        if (!hold && character == player && _responseGuards.Contains(container)
            && NearbyStorage.IsAccessible(container, player))
        {
            _manualOpens[container] = new ManualOpen
            {
                Player = player,
                Deadline = Time.realtimeSinceStartup + OwnershipTimeout
            };
        }
    }

    internal static void Forget(Container container)
    {
        _requests.Remove(container);
        _manualOpens.Remove(container);
        _responseGuards.Remove(container);
    }

    internal static bool ReceiveResponse(Container container, bool granted)
    {
        if (_requests.TryGetValue(container, out var request))
        {
            request.Responded = true;
            request.Granted = granted;

            return true;
        }

        if (_manualOpens.TryGetValue(container, out var manual))
        {
            _manualOpens.Remove(container);
            var player = manual.Player;

            if (Time.realtimeSinceStartup < manual.Deadline && player != null
                && player == Player.m_localPlayer && !player.IsDead()
                && NearbyStorage.IsAccessible(container, player)
                && (container.transform.position - player.transform.position).sqrMagnitude
                    <= (player.m_maxInteractDistance * player.m_maxInteractDistance)
                && (!granted || container.IsOwner()))
            {
                return false;
            }

            Failed(player, "Storage response arrived after manual access became invalid; opening cancelled.");
        }

        return _responseGuards.Contains(container);
    }

    internal static void Cancel()
    {
        _cancelled = true;
        Release();
    }

    internal static void Clear()
    {
        Cancel();
        _requests.Clear();
        _manualOpens.Clear();
        _responseGuards.Clear();
        IsExecuting = false;
        IsPending = false;
    }

    internal static IEnumerator Craft(InventoryGui gui, Player player)
    {
        IsPending = true;
        _cancelled = false;
        Recipe recipe = gui.m_craftRecipe;
        var upgrade = gui.m_craftUpgradeItem;
        var station = player.GetCurrentCraftingStation();
        int quality = upgrade == null ? 1 : upgrade.m_quality + 1;
        int multiplier = gui.m_multiCrafting ? gui.m_multiCraftAmount : 1;
        List<Container> sources;

        try
        {
            using (var preview = new CraftingContext(player, NearbyStorage.Find(player, true)))
            {
                var plan = IngredientPlan.Create(preview, recipe, quality, multiplier);

                if (plan == null)
                {
                    Failed(player, "Insufficient ingredients when crafting completed; no ingredients were consumed.");

                    yield break;
                }

                sources = plan.StorageSources(preview);
            }

            foreach (var container in sources)
            {
                if (!Valid(gui, player, recipe, upgrade, station))
                {
                    yield break;
                }

                if (!NearbyStorage.IsAccessible(container, player))
                {
                    Failed(player, "Storage changed before crafting; no ingredients were consumed.");

                    yield break;
                }

                if (IsBusy(container))
                {
                    Failed(player, "Storage has another interaction pending; crafting cancelled.");

                    yield break;
                }

                if (!container.IsOwner())
                {
                    var request = new Request();
                    _requests.Add(container, request);
                    _responseGuards.Add(container);
                    container.m_nview.InvokeRPC("RPC_RequestOpen", player.GetPlayerID());
                    float deadline = Time.realtimeSinceStartup + OwnershipTimeout;

                    while (true)
                    {
                        if (!Valid(gui, player, recipe, upgrade, station) || container == null
                            || container.m_nview == null || !container.m_nview.IsValid())
                        {
                            Failed(player, "Crafting or storage became invalid while awaiting ownership; crafting cancelled.");

                            yield break;
                        }

                        if ((request.Responded && !request.Granted) || Time.realtimeSinceStartup >= deadline)
                        {
                            Failed(player, "Storage ownership was denied or timed out; no ingredients were consumed.");

                            yield break;
                        }

                        if (request.Responded && container.IsOwner())
                        {
                            break;
                        }

                        yield return null;
                    }

                    _requests.Remove(container);
                }

                if (!NearbyStorage.IsAccessible(container, player))
                {
                    Failed(player, "Storage became inaccessible while awaiting ownership; crafting cancelled.");

                    yield break;
                }

                container.Load();
                container.m_inUse = true;
                _reserved.Add(container);
            }

            if (!Valid(gui, player, recipe, upgrade, station))
            {
                yield break;
            }

            foreach (var container in sources)
            {
                if (!NearbyStorage.IsAccessible(container, player, true) || !container.IsOwner())
                {
                    Failed(player, "Reserved storage is no longer owned or accessible; crafting cancelled.");

                    yield break;
                }
            }

            using var context = new CraftingContext(player, sources)
            {
                Reserved = true
            };

            context.Plan = IngredientPlan.Create(context, recipe, quality, multiplier);

            if (context.Plan == null)
            {
                Failed(player, "Insufficient ingredients after refreshing storage; no ingredients were consumed.");

                yield break;
            }

            IsExecuting = true;
            gui.m_multiCraftAmount = multiplier;
            gui.DoCrafting(player);
        }
        finally
        {
            IsExecuting = false;
            IsPending = false;
            _requests.Clear();
            Release();
        }
    }

    private static void Release()
    {
        foreach (var container in _reserved)
        {
            if (container != null)
            {
                // Reserve only the owner's local busy flag, without opening a chest or playing effects.
                container.m_inUse = false;
            }
        }

        _reserved.Clear();
    }

    private static bool Valid(
        InventoryGui gui,
        Player player,
        Recipe recipe,
        ItemDrop.ItemData upgrade,
        CraftingStation station)
    {
        return !_cancelled && gui != null && InventoryGui.IsVisible() && player != null
            && player == Player.m_localPlayer && !player.IsDead()
            && gui.m_craftRecipe == recipe && gui.m_craftUpgradeItem == upgrade
            && gui.m_selectedRecipe.Recipe == recipe && gui.m_selectedRecipe.ItemData == upgrade
            && player.GetCurrentCraftingStation() == station;
    }

    private static void Failed(Player player, string reason)
    {
        CraftFromStoragePlugin.Log.LogWarning(reason);

        if (player != null)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_missingrequirement", 0, null, false);
        }
    }

    private sealed class Request
    {
        internal bool Responded { get; set; }

        internal bool Granted { get; set; }
    }

    private sealed class ManualOpen
    {
        internal Player Player { get; set; }

        internal float Deadline { get; set; }
    }
}