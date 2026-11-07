# SupplyFromStorage

Add one accepted fuel or processing input to a nearby Valheim machine when using its default E interaction.

By Hoose.

## Features

- Uses a fixed 20 metre sphere centered on the local player.
- Supplies one fuel item per valid E action for fires and lights, cooking-station fuel slots, smelters and blast furnaces.
- Supplies one currently accepted Smelter input per valid E action, including charcoal kiln wood, smelter ores, windmill barley, spinning-wheel flax and eitr-refinery inputs.
- Holding E repeats one-item transfers at the game's interaction repeat interval; stations are not filled on load or during updates.
- Does not transfer cooking ingredients, fermenter bases, outputs, unrelated items or player-inventory items.
- Fuel and processing-input automation have independent settings, both enabled by default.
- Rechecks normal container access, ward/busy rules, local ownership and station capacity before consuming.

Requires BepInExPack_Valheim. Install on the client. The mod skips remote-owned containers and machines rather than risking a remote inventory debit. Multiplayer behavior requires in-game validation.

Configuration: `BepInEx/config/dhamhuis.valheim.supplyfromstorage.cfg`.

This is a local preview, not an in-game-certified release. Stub tests do not prove live networking or persistence.

[Project and support](https://github.com/dhamhuis/ValheimMods)
