# Isaiahfeldt's FFXIV plugins

One custom Dalamud repository for these plugins:

| Plugin | Purpose | Command |
| --- | --- | --- |
| Dagobert | Marketboard pricing with duplicate-item caching and scheduled runs | `/dagobert` |
| Artisan (fork) | Crafting fork with ingredient marketboard open-and-fill shortcut | `/artisan` |
| Dualcast Gem | Movable glowing Red Mage Dualcast indicator | `/dualcastgem` |
| Auto Grit | Configurable stance for all tanks on combat or PvE duty entry | `/autogrit` |

## Install

In Dalamud Settings, open Experimental / Custom Plugin Repositories and add:

```text
https://raw.githubusercontent.com/Isaiahfeldt/ffxiv/main/pluginmaster.json
```

Save, then open `/xlplugins` and install the plugins you want. You only need this one URL for all plugins hosted here.

When migrating from the old Dagobert-specific repository, back up your configuration, remove its old custom repository URL, and reinstall Dagobert from this repository if Dalamud still associates it with the old source. Do not load duplicate developer copies alongside installed plugins.

Both initial packages compile against Dalamud API 15. Their in-game automation still requires testing. Auto Grit applies the relevant stance for Gladiator/Paladin, Marauder/Warrior, Dark Knight, and Gunbreaker. Configure trigger toggles, jobs, delays, other-tank head start, retries, and chat messages with `/autogrit`. Disable it when you intend to off-tank without stance.

## Publish a plugin or update

Dualcast Gem source and testing instructions are available in [src/DualcastGem](src/DualcastGem/README.md). Version 0.1.0.0 is available in the plugin feed for in-game testing. Open `/dualcastgem` to configure or preview the gem.

Auto Grit source and behavior checks live in [src/AutoGrit](src/AutoGrit/README.md). Dagobert source lives in [Isaiahfeldt/Dagobert](https://github.com/Isaiahfeldt/Dagobert). This repository hosts their packages and shared installation manifest.

1. Build and test the plugin; increase its four-part assembly version for each update.
2. From this repository, import the resulting package:

   ```sh
   python3 scripts/update_repository.py /path/to/latest.zip
   ```

   You can supply several packages at once. This preserves other plugins and retains previous packages. The highest numeric version of each plugin is advertised; importing an older version will not downgrade it. Published package contents cannot be replaced without increasing the version.
3. Review `pluginmaster.json`, then commit and push the manifest and `plugins/` changes to `main`.
4. In Dalamud, refresh the plugin list and check for updates.

GitHub Releases and GitHub Pages are optional. All downloads come directly from versioned files in this repository. No separate plugin URL needs to be added in Dalamud for future plugins.

This updater maintains this central index. Individual plugin release workflows must publish here to automate updates; the existing Dagobert workflow currently still updates its older per-plugin repository.

## Scheduled Dagobert runs

Update Dagobert to 1.14.2.0 or newer. In `/dagobert` → **Scheduled Auto Pinch**, enable scheduling and set an interval in minutes (60 = one hour). Park within reach of a summoning bell with menus closed. **Run now** starts the first cycle when available; **Cancel and pause** stops automation until you resume it in this plugin session.

The plugin opens the bell, reprices enabled retainers, exits the bell, and waits the configured interval after finishing. It does not move or teleport. Busy/out-of-range characters wait; errors or interruptions during a run pause scheduling. Fresh prices are fetched each cycle, with duplicate-item cache reuse within that cycle. Bell interaction and repeated cycles still require in-game testing.

Dagobert 1.14.2.1 adds **Frequency** and **Random extra delay** sliders. A 60-minute frequency plus up to 10 extra minutes waits 60–70 minutes between cycles, measured after each completed run. Zero disables randomness; Run now bypasses the wait.

## Artisan ingredient marketboard shortcut

Artisan **4.0.5.26** removes the toggle. Ctrl+left-click an ingredient name or choose **Search in game marketboard** from its context menu. The shortcut opens a reachable board or uses an open one, then fills the name. **Submit manually with Enter or Search.** Automatic submission has been removed. Shift-click copying remains unchanged.

Source: [Isaiahfeldt/Artisan](https://github.com/Isaiahfeldt/Artisan). The package includes its original BSD license.
