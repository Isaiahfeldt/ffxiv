# Dualcast Gem

A transparent, movable gem for Red Mage. The approved gray/ruby artwork is shipped as one PNG atlas: gray when Dualcast (status 1249) is absent, red while present. Swiftcast and other instant-cast effects do not activate it. This plugin only reads statuses and draws a HUD element; it performs no game actions.

Open `/dualcastgem` to enable/disable, lock position, set size (40–300 pixels) and opacity, restrict to combat, or hide the inactive gem. Unlock and drag to position; lock makes the overlay click-through. Position is saved on release and normalized to the viewport so resolution changes keep it on screen. Reset position restores the default. The preview can display either state on any job while settings are open and does not persist across reloads.

The live gem is Red Mage only and hidden during logout, loading and cutscenes. Dalamud's normal UI hiding also applies. Defaults: enabled, unlocked, 100 pixels, full opacity, visible outside combat and when inactive.

## Build and check

With .NET 10 and current Dalamud API 15 libraries available via `DALAMUD_HOME`:

```sh
dotnet build DualcastGem/DualcastGem.csproj -c Release -p:EnableWindowsTargeting=true
dotnet run --project DualcastGem.Checks
```

The release package must contain `DualcastGem.dll`, `DualcastGem.json`, and `Assets/gems.png`. Keep the assets directory alongside the DLL when using Dalamud's developer plugin locations. Do not load a developer copy alongside an installed copy. Publishing to the central repository uses the existing updater only after release approval.

## In-game acceptance tests

- On Red Mage, cast a spell that grants Dualcast: gray changes to red. Consume it with a spell: red returns to gray. Let it expire and verify the same transition.
- Swiftcast alone must leave the gem gray. Check job changes, logout/login, death, loading, cutscenes and hiding the game UI for stale indicators.
- Unlock, drag, lock, and verify clicks pass through. Reload the plugin and change resolution; confirm saved position and size. Use Reset position if needed.
- Test combat-only and hide-inactive separately and together. Preview both states on another job, then close settings and confirm normal visibility resumes.
- Check both images align at small and large sizes, without a background rectangle or clipped tips. Reload/unload the plugin and check the Dalamud log for errors.

Artwork: generated for this plugin and approved by the repository owner; the original transparent atlas is included unchanged. No game asset is extracted or redistributed.
