using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Textures;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace DualcastGem;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IObjectTable Objects { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static ITextureProvider Textures { get; private set; } = null!;

    private readonly Configuration config;
    private readonly ISharedImmediateTexture atlas;
    private readonly ISharedImmediateTexture swiftcastTexture;
    private bool settingsOpen;
    private bool preview;
    private int previewMode = (int)GemMode.Dualcast;
    private bool positionDirty;
    private string? textureError;

    public Plugin()
    {
        config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        config.Normalize();
        atlas = Textures.GetFromFile(Path.Combine(PluginInterface.AssemblyLocation.DirectoryName!, "Assets", "gems.png"));
        swiftcastTexture = Textures.GetFromFile(Path.Combine(PluginInterface.AssemblyLocation.DirectoryName!, "Assets", "swiftcast.png"));
        Commands.AddHandler("/dualcastgem", new CommandInfo(OnCommand) { HelpMessage = "Configure and position the Dualcast gem." });
        PluginInterface.UiBuilder.Draw += Draw;
        PluginInterface.UiBuilder.OpenConfigUi += OpenSettings;
        PluginInterface.UiBuilder.OpenMainUi += OpenSettings;
    }

    private void OnCommand(string command, string args) => OpenSettings();
    private void OpenSettings() => settingsOpen = true;
    private void Save()
    {
        config.Normalize();
        PluginInterface.SavePluginConfig(config);
    }

    private void Draw()
    {
        if (settingsOpen) DrawSettings();
        if (!settingsOpen) preview = false;

        var player = Objects.LocalPlayer;
        var mode = player == null ? GemMode.Inactive : GemState.Resolve(player.StatusList.Select(s => s.StatusId));
        var blocked = Condition[ConditionFlag.BetweenAreas] || Condition[ConditionFlag.BetweenAreas51]
            || Condition[ConditionFlag.WatchingCutscene] || Condition[ConditionFlag.WatchingCutscene78];
        var showPreview = settingsOpen && preview && ClientState.IsLoggedIn && !blocked;
        if (showPreview || GemState.IsVisible(config.Enabled, ClientState.IsLoggedIn,
                player?.ClassJob.RowId ?? 0, Condition[ConditionFlag.InCombat], config.CombatOnly,
                config.HideWhenInactive, mode != GemMode.Inactive, blocked))
            DrawGem(showPreview ? (GemMode)previewMode : mode);

        // Persist once after a drag, including when the gem becomes hidden mid-drag.
        if (positionDirty && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            positionDirty = false;
            Save();
        }
    }

    private void DrawGem(GemMode mode)
    {
        var source = mode == GemMode.Swiftcast ? swiftcastTexture : atlas;
        if (!source.TryGetWrap(out var texture, out var error))
        {
            textureError = error == null ? null : "Unable to load gem artwork. Reinstall the complete plugin package.";
            return;
        }
        textureError = null;
        var viewport = ImGui.GetMainViewport();
        var side = MathF.Min(config.Size, MathF.Min(viewport.Size.X, viewport.Size.Y));
        var size = new Vector2(side);
        var available = Vector2.Max(Vector2.Zero, viewport.Size - size);
        var pos = viewport.Pos + new Vector2(available.X * config.PositionX, available.Y * config.PositionY);
        ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoScrollWithMouse;
        if (config.Locked) flags |= ImGuiWindowFlags.NoInputs;
        if (ImGui.Begin("##DualcastGemIndicator", flags))
        {
            // Equal square UV regions align both gems without modifying the approved atlas.
            var left = mode == GemMode.Dualcast ? 1257f : 270f;
            var uv0 = new Vector2(left / 2172f, 16f / 724f);
            var uv1 = new Vector2((left + 648f) / 2172f, 664f / 724f);
            if (mode == GemMode.Swiftcast)
            {
                // Match the blue artwork's visible frame bounds to the original atlas.
                uv0 = new Vector2(85f / 1278f, 3f / 1230f);
                uv1 = new Vector2(1192f / 1278f, 1175f / 1230f);
            }
            ImGui.GetWindowDrawList().AddImage(texture.Handle, pos, pos + size, uv0, uv1,
                ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, config.Opacity)));
            if (!config.Locked)
            {
                ImGui.InvisibleButton("##DragGem", size);
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    var delta = ImGui.GetIO().MouseDelta;
                    config.PositionX = Math.Clamp(config.PositionX + delta.X / MathF.Max(1, available.X), 0, 1);
                    config.PositionY = Math.Clamp(config.PositionY + delta.Y / MathF.Max(1, available.Y), 0, 1);
                    positionDirty = true;
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Drag to move. Lock in /dualcastgem for click-through.");
            }
        }
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawSettings()
    {
        ImGui.SetNextWindowSize(new Vector2(420, 360), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Dualcast Gem", ref settingsOpen, ImGuiWindowFlags.NoCollapse))
        {
            var changed = ImGui.Checkbox("Enabled", ref config.Enabled);
            changed |= ImGui.Checkbox("Lock position (click-through)", ref config.Locked);
            changed |= ImGui.Checkbox("Only show in combat", ref config.CombatOnly);
            changed |= ImGui.Checkbox("Hide when neither buff is active", ref config.HideWhenInactive);
            changed |= ImGui.SliderFloat("Size", ref config.Size, 40, 300, "%.0f px");
            changed |= ImGui.SliderFloat("Opacity", ref config.Opacity, 0.15f, 1, "%.2f");
            if (ImGui.Button("Reset position"))
            {
                config.PositionX = 0.55f;
                config.PositionY = 0.65f;
                changed = true;
            }
            ImGui.Separator();
            ImGui.Checkbox("Preview gem (any job)", ref preview);
            if (preview) ImGui.Combo("Preview state", ref previewMode, "Inactive (gray)\0Dualcast (red)\0Swiftcast (blue)\0");
            ImGui.TextWrapped("Unlock and drag the gem to position it. Preview ends when this window closes. Normally the gem appears only on Red Mage: blue for Swiftcast, red for Dualcast, gray otherwise. Swiftcast takes priority when both buffs are active.");
            if (textureError != null) ImGui.TextWrapped(textureError);
            if (changed) Save();
        }
        ImGui.End();
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenSettings;
        PluginInterface.UiBuilder.OpenMainUi -= OpenSettings;
        Commands.RemoveHandler("/dualcastgem");
        if (positionDirty) Save();
        // The shared texture and per-frame wraps are owned by Dalamud.
    }
}
