# Isaiahfeldt's FFXIV plugins

One custom Dalamud repository for these plugins:

| Plugin | Purpose | Command |
| --- | --- | --- |
| Dagobert | Marketboard pricing automation with cached prices for duplicate items | `/dagobert` |
| Auto Grit | Appropriate stance for all tanks on combat start or regular dungeon entry | `/autogrit` |

## Install

In Dalamud Settings, open Experimental / Custom Plugin Repositories and add:

```text
https://raw.githubusercontent.com/Isaiahfeldt/ffxiv/main/pluginmaster.json
```

Save, then open `/xlplugins` and install the plugins you want. You only need this one URL for all plugins hosted here.

When migrating from the old Dagobert-specific repository, back up your configuration, remove its old custom repository URL, and reinstall Dagobert from this repository if Dalamud still associates it with the old source. Do not load duplicate developer copies alongside installed plugins.

Both initial packages compile against Dalamud API 15. Their in-game automation still requires testing. Auto Grit applies the relevant stance for Gladiator/Paladin, Marauder/Warrior, Dark Knight, and Gunbreaker. Disable it when you intend to off-tank without stance.

## Publish a plugin or update

Keep plugin source in its own project or repository. This repository hosts built packages and the shared installation manifest; it does not need each plugin's source.

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
