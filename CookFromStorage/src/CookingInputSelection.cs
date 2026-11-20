using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimMods.Common.InputSelection;

namespace CookFromStorage;

internal static class CookingInputSelection
{
    internal static InputTypeSession Session { get; } = new(continueAcrossTargets: true);

    internal static InputPreferences Preferences { get; set; }

    internal static string PreferenceStationType { get; set; }

    internal static string StationType(Component station)
    {
        return station switch
        {
            CookingStation cooking => cooking.m_name,
            Fermenter fermenter => fermenter.m_name,
            _ => null
        };
    }

    internal static IReadOnlyList<ItemDrop> Inputs(Component station)
    {
        IEnumerable<ItemDrop> conversions = station switch
        {
            CookingStation cooking when cooking.m_name != "$piece_frostfoundry" && cooking.m_conversion != null
                => cooking.m_conversion.Select(conversion => conversion?.m_from),
            Fermenter fermenter when fermenter.m_conversion != null
                => fermenter.m_conversion.Select(conversion => conversion?.m_from),
            _ => Enumerable.Empty<ItemDrop>()
        };

        return conversions.Where(item => item?.m_itemData?.m_shared != null
                && !string.IsNullOrEmpty(item.name) && IsAllowed(station, item.name, item.m_itemData.m_shared.m_name))
            .GroupBy(item => item.name, StringComparer.Ordinal).Select(group => group.First()).ToArray();
    }

    internal static bool Supports(Component station) => !string.IsNullOrEmpty(StationType(station)) && Inputs(station).Count > 1;

    internal static string DefaultPreferences(Component station) => string.Join(",", Inputs(station).Select(item => item.name)) + "|";

    internal static bool CanUse(Component station, Player player)
    {
        return station switch
        {
            CookingStation cooking => CookingTransfer.CanUse(cooking, player)
                && PrivateArea.CheckAccess(cooking.transform.position, 0f, false, false),
            Fermenter fermenter => FermenterTransfer.CanUse(fermenter, player)
                && PrivateArea.CheckAccess(fermenter.transform.position, 0f, false, false),
            _ => false
        };
    }

    internal static bool TryHandle(Component station, Humanoid user, out bool result)
    {
        result = false;

        if (!CookFromStoragePlugin.InputSelectionEnabled || !CookFromStoragePlugin.AutomaticIngredientsEnabled
            || user != Player.m_localPlayer
            || user is not Player player || player.GetInventory() == null || station == null
            || (!Supports(station) && !ReferenceEquals(Session.Context, station)))
        {
            return false;
        }

        if ((station is CookingStation cooking && cooking.HaveDoneItem())
            || (station is Fermenter fermenter && fermenter.GetStatus() != Fermenter.Status.Empty))
        {
            return false;
        }

        if (!Session.CanInsert || !ReferenceEquals(Session.Context, station))
        {
            return true;
        }

        if (!CanUse(station, player))
        {
            Notify(player, "Station input stopped: target or access changed. Release use before trying again.");

            return true;
        }

        if (Preferences == null || PreferenceStationType != StationType(station))
        {
            Notify(player, "Station preferences are invalid or unavailable. Insertion blocked; check the log/config.");

            return true;
        }

        if (station is Fermenter sheltered)
        {
            sheltered.UpdateCover(0f, true);

            if (!sheltered.m_hasRoof || sheltered.m_exposed)
            {
                Notify(player, !sheltered.m_hasRoof ? "$piece_fermenter_needroof" : "$piece_fermenter_exposed");

                return true;
            }
        }

        var sources = NearbyStorage.Find(player);
        var inputs = Inputs(station);
        var available = inputs.Where(item => player.IsMaterialKnown(item.m_itemData.m_shared.m_name))
            .Select(item => item.name)
            .Where(name => InputPreviewItem.Count(player.GetInventory(), name) > 0
                || sources.Any(source => StorageInputTransfer.TryGetInventory(source, player, out var inventory)
                    && InputPreviewItem.Count(inventory, name) > 0))
            .ToList();
        string chosen = Session.Resolve(Preferences, available);

        if (chosen == null)
        {
            // Match vanilla when nothing accepted exists; only explain when Never auto-add blocked real items.
            string vanillaMessage = station is CookingStation emptyRack ? emptyRack.m_noCookableItemsMessage : "$msg_noprocessableitems";
            Notify(player, available.Count == 0 ? vanillaMessage
                : $"Items set to Never auto-add ({CookFromStoragePlugin.InputPickerKeyName})");

            return true;
        }

        var chosenInput = inputs.FirstOrDefault(item => item.name == chosen);

        if (chosenInput == null || !player.IsMaterialKnown(chosenInput.m_itemData.m_shared.m_name))
        {
            Notify(player, "Selected input is not discovered or no longer accepted. Release use before trying again.");

            return true;
        }

        if (!IsReady(station))
        {
            string message = station is CookingStation rack
                ? rack.m_requireFire && !rack.IsFireLit() ? "$msg_needfire" : "$msg_nocookroom"
                : "Station input stopped: the Fermenter is not ready. Release use before trying again.";
            Notify(player, message);

            return true;
        }

        var playerInventory = player.GetInventory();
        var exactItem = FindExact(playerInventory, chosen);

        if (exactItem != null)
        {
            result = Transfer(station, player, null, playerInventory, exactItem, chosen);
        }
        else
        {
            foreach (var source in sources)
            {
                if (!StorageInputTransfer.TryGetInventory(source, player, out var inventory))
                {
                    continue;
                }

                exactItem = FindExact(inventory, chosen);

                if (exactItem != null)
                {
                    result = Transfer(station, player, source, inventory, exactItem, chosen);

                    break;
                }
            }
        }

        if (!result)
        {
            var input = Inputs(station).FirstOrDefault(item => item.name == chosen);
            string name = input == null ? "selected item" : Localization.instance.Localize(input.m_itemData.m_shared.m_name);
            Notify(player, exactItem == null
                ? $"No {name} available. Release use before trying again."
                : "Station input stopped: transfer failed. Release use before trying again.");
        }

        return true;
    }

    private static bool IsAllowed(Component station, string prefabName, string sharedName)
    {
        return station switch
        {
            CookingStation cooking => cooking.IsItemAllowed(prefabName)
                && cooking.m_incompatibleItems?.Any(message => message?.m_item?.m_itemData?.m_shared?.m_name == sharedName) != true,
            Fermenter fermenter => fermenter.IsItemAllowed(prefabName.GetStableHashCode()),
            _ => false
        };
    }

    private static bool IsReady(Component station)
    {
        return station switch
        {
            CookingStation cooking => !cooking.HaveDoneItem() && cooking.GetFreeSlot() >= 0
                && (!cooking.m_requireFire || cooking.IsFireLit()),
            Fermenter fermenter => fermenter.GetStatus() == Fermenter.Status.Empty
                && fermenter.m_hasRoof && !fermenter.m_exposed,
            _ => false
        };
    }

    private static ItemDrop.ItemData FindExact(Inventory inventory, string prefabName)
    {
        return inventory.GetAllItems().FirstOrDefault(item => item?.m_shared != null && item.m_stack > 0
            && item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName);
    }

    private static bool Transfer(
        Component station,
        Player player,
        Container source,
        Inventory inventory,
        ItemDrop.ItemData item,
        string prefabName)
    {
        int freeSlot = station is CookingStation cooking ? cooking.GetFreeSlot() : -1;
        int hash = prefabName.GetStableHashCode();

        return StorageInputTransfer.ConsumeOne(
            source,
            inventory,
            player,
            item,
            StationType(station),
            () => CanUse(station, player) && IsReady(station) && IsAllowed(station, prefabName, item.m_shared.m_name)
                && player.IsMaterialKnown(item.m_shared.m_name)
                && item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName
                && (station is not CookingStation rack || rack.GetFreeSlot() == freeSlot),
            () =>
            {
                if (station is CookingStation rack)
                {
                    rack.RPC_AddItem(player.GetPlayerID(), prefabName, item.m_cheated);
                }
                else if (station is Fermenter fermenter)
                {
                    fermenter.RPC_AddItem(player.GetPlayerID(), hash, item.m_cheated);
                }
            },
            () => station is CookingStation rack
                ? rack.GetFreeSlot() == -1 || rack.GetFreeSlot() > freeSlot
                : station is Fermenter fermenter && fermenter.GetContent() == hash);
    }

    private static void Notify(Player player, string message)
    {
        if (Session.Stop())
        {
            player.Message(MessageHud.MessageType.Center, message, 0, null, false);
        }
    }
}