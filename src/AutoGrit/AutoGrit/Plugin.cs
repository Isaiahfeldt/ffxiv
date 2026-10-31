using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.ClientState.Objects.Types;
using TerritoryType = Lumina.Excel.Sheets.TerritoryType;

namespace AutoGrit;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IObjectTable Objects { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static IPartyList Party { get; private set; } = null!;
    [PluginService] internal static IDataManager Data { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly CombatStartController controller = new();
    private readonly Configuration configuration;
    private readonly WindowSystem windows = new("AutoGrit");
    private readonly ConfigWindow configWindow;

    public Plugin()
    {
        configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        configWindow = new ConfigWindow(configuration, SaveConfiguration);
        windows.AddWindow(configWindow);
        Commands.AddHandler("/autogrit", new CommandInfo(OnCommand)
        {
            HelpMessage = "Configure Auto Grit. /autogrit on or off enables or disables it.",
        });
        Framework.Update += OnUpdate;
        PluginInterface.UiBuilder.Draw += Draw;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi += OpenConfig;
    }

    private unsafe void OnUpdate(IFramework framework)
    {
        var player = Objects.LocalPlayer;
        if (player == null)
        {
            controller.Reset();
            return;
        }

        var stance = TankStance.ForJob(player.ClassJob.RowId);
        var eligible = configuration.Enabled && !ClientState.IsPvP
            && stance.HasValue && configuration.IsJobEnabled(player.ClassJob.RowId) && player.Level >= 10;
        var hasStance = stance.HasValue && player.StatusList.Any(status => status.StatusId == stance.Value.StatusId);
        var canAct = player.CurrentHp > 0 && player.IsTargetable
            && !Condition[ConditionFlag.BetweenAreas] && !Condition[ConditionFlag.WatchingCutscene]
            && !Condition[ConditionFlag.WatchingCutscene78];
        var duty = GetDutyTerritory();
        var anotherTankHasStance = duty != 0 && configuration.DelayWhenOtherTankHasStance
            && Party.Any(member => member.GameObject is IBattleChara other
                && other.GameObjectId != player.GameObjectId && other.CurrentHp > 0
                && TankStance.ForJob(member.ClassJob.RowId) is { } otherStance
                && other.StatusList.Any(status => status.StatusId == otherStance.StatusId));
        if (!controller.ShouldAttempt(Environment.TickCount64, Condition[ConditionFlag.InCombat], eligible, hasStance, duty, canAct, configuration.GetOptions(), anotherTankHasStance)
            || !stance.HasValue)
            return;

        var actions = ActionManager.Instance();
        if (actions == null || actions->GetActionStatus(ActionType.Action, stance.Value.ActionId) != 0)
            return;

        // One accepted action per combat entry: never toggle again while waiting
        // for the server's Grit status update.
        if (actions->UseAction(ActionType.Action, stance.Value.ActionId, player.GameObjectId))
        {
            controller.ActionAccepted();
            Log.Debug("Auto Grit activated {Stance} after combat or duty entry.", stance.Value.Name);
            if (configuration.ShowActivationMessage)
                Chat.Print($"[Auto Grit] Activated {stance.Value.Name}.");
        }
    }

    private uint GetDutyTerritory()
    {
        if (ClientState.IsPvP)
            return 0;
        if (Condition[ConditionFlag.BoundByDuty] || Condition[ConditionFlag.BoundByDuty56]
            || Condition[ConditionFlag.BoundByDuty95])
            return ClientState.TerritoryType;
        if (!Data.GetExcelSheet<TerritoryType>().TryGetRow(ClientState.TerritoryType, out var territory))
            return 0;
        // Roulette is a queue: the destination's duty determines the behavior.
        return territory.ContentFinderCondition.RowId != 0 ? ClientState.TerritoryType : 0;
    }

    private void OnCommand(string command, string arguments)
    {
        switch (arguments.Trim().ToLowerInvariant())
        {
            case "on":
                configuration.Enabled = true;
                SaveConfiguration();
                Chat.Print("[Auto Grit] Enabled for the next combat or duty entry.");
                break;
            case "off":
                configuration.Enabled = false;
                SaveConfiguration();
                Chat.Print("[Auto Grit] Disabled.");
                break;
            default:
                OpenConfig();
                break;
        }
    }

    private void SaveConfiguration() => PluginInterface.SavePluginConfig(configuration);
    private void Draw() => windows.Draw();
    private void OpenConfig() => configWindow.IsOpen = true;

    public void Dispose()
    {
        Framework.Update -= OnUpdate;
        PluginInterface.UiBuilder.Draw -= Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi -= OpenConfig;
        Commands.RemoveHandler("/autogrit");
        windows.RemoveAllWindows();
    }

    private sealed class ConfigWindow(Configuration config, Action save) : Window("Auto Grit Settings")
    {
        public override void Draw()
        {
            Toggle("Enable automatic tank stance", config.Enabled, value => config.Enabled = value);
            Toggle("Enable on combat start", config.EnableOnCombatStart, value => config.EnableOnCombatStart = value);
            Toggle("Enable on duty entry", config.EnableOnDutyEntry, value => config.EnableOnDutyEntry = value);
            ImGui.TextWrapped("Duties include dungeons, trials, raids, and other PvE instances, including roulette destinations. PvP is excluded.");
            ImGui.Separator();
            ImGui.TextUnformatted("Enabled jobs");
            Toggle("Gladiator / Paladin", config.EnablePaladin, value => config.EnablePaladin = value);
            Toggle("Marauder / Warrior", config.EnableWarrior, value => config.EnableWarrior = value);
            Toggle("Dark Knight", config.EnableDarkKnight, value => config.EnableDarkKnight = value);
            Toggle("Gunbreaker", config.EnableGunbreaker, value => config.EnableGunbreaker = value);
            ImGui.Separator();
            Seconds("Combat activation delay (seconds)", config.CombatDelaySeconds, 60, value => config.CombatDelaySeconds = value);
            Toggle("Delay when another tank has stance (duties only)", config.DelayWhenOtherTankHasStance,
                value => config.DelayWhenOtherTankHasStance = value);
            if (config.DelayWhenOtherTankHasStance)
            {
                Seconds("Other tank head start (seconds)", config.OtherTankDelaySeconds, 60, value => config.OtherTankDelaySeconds = value);
                ImGui.TextWrapped("Waits at least this long after combat starts. If another tank has stance on duty entry, entry activation is deferred to combat. If combat activation is disabled, it stays off in that situation.");
                ImGui.TextWrapped("Gives the other tank time to build enmity; it cannot guarantee who keeps aggro. Your existing stance is never turned off.");
            }
            Seconds("Retry duration after combat delay (seconds)", config.RetryDurationSeconds, 30, value => config.RetryDurationSeconds = value, 1);
            Toggle("Show activation message in chat", config.ShowActivationMessage, value => config.ShowActivationMessage = value);
            ImGui.TextWrapped("Existing stance is left alone. Automation does not continuously maintain stance during combat.");
        }

        private void Toggle(string label, bool current, Action<bool> set)
        {
            if (!ImGui.Checkbox(label, ref current)) return;
            set(current);
            save();
        }

        private void Seconds(string label, float current, float maximum, Action<float> set, float minimum = 0)
        {
            if (!ImGui.SliderFloat(label, ref current, minimum, maximum, "%.1f")) return;
            set(current);
            save();
        }
    }
}
