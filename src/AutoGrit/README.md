# Auto Grit

Standalone Dalamud API 15 tank stance automation. Configure with `/autogrit`, or use `/autogrit on` and `/autogrit off`.

| Setting | Default |
| --- | --- |
| Enable plugin | On |
| Enable on combat start | On |
| Enable on duty entry | On |
| Gladiator / Paladin, Marauder / Warrior, Dark Knight, Gunbreaker | All enabled |
| Combat activation delay | 0 seconds (0–60) |
| Delay when another party tank has stance, in duties only | On |
| Other tank head start | 10 seconds (0–60) |
| Retry duration after combat delay | 5 seconds (1–30) |
| Show activation message | Off |

Duty entry includes dungeons, trials, raids, and other instanced PvE duties, including roulette destinations. Roulette is a queue, so the destination determines the duty. PvP is excluded from all automation.

Auto Grit uses Iron Will, Defiance, Grit, or Royal Guard as appropriate. Stance must be unlocked (level 10 or higher). Existing stance is left alone; automation never turns it off or continuously maintains it during a fight.

If another living tank in your party already has stance, the duty-only head start waits until the configured number of seconds after combat begins. The larger of this delay and your normal combat delay is used. A pending activation also respects another tank gaining stance before your action is accepted. This gives that tank a head start building enmity, but does not guarantee they keep aggro. Party members must be loaded and their status visible to the client for detection.

Duty-entry activation is deferred to combat if another party tank already has stance. If combat activation is disabled, no deferred activation occurs in that situation. If your own stance was already enabled before the other tank, it stays enabled. To intentionally off-tank without stance, disable automation.

Dungeon/duty entry waits up to two minutes for loading/cutscenes. Combat retries run for the configured duration after the activation delay, at most once per half second. Once an action is accepted there are no further attempts for that trigger. Enabling automation mid-fight waits for the next combat/duty entry.

## Install

Add this custom Dalamud repository URL:

```text
https://raw.githubusercontent.com/Isaiahfeldt/ffxiv/main/pluginmaster.json
```

Install or update Auto Grit through `/xlplugins`. For manual developer installation, extract the built `AutoGrit/bin/x64/Release/AutoGrit/latest.zip` to a permanent folder and add its `AutoGrit.dll` to Dalamud Dev Plugin Locations. Do not load both installed and developer copies at once.

## Build and check

Install .NET 10 and set `DALAMUD_HOME` to a current stable Dalamud installation containing `Dalamud.dll`. From this directory:

```sh
dotnet restore AutoGrit/AutoGrit.csproj --locked-mode -p:EnableWindowsTargeting=true
dotnet build AutoGrit/AutoGrit.csproj --no-restore -c Release -p:Platform=x64 -p:EnableWindowsTargeting=true
dotnet run --project AutoGrit.Checks
```

The plugin builds without ECommons. Automated checks cover combat/duty triggers, retries, delays, other-tank handling, and stance protection. In-game action execution, settings persistence, and duty transitions still require testing in FFXIV.
