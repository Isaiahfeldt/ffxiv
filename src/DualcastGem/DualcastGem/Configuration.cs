using System;
using Dalamud.Configuration;

namespace DualcastGem;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Enabled = true;
    public bool Locked;
    public bool CombatOnly;
    public bool HideWhenInactive;
    public float Size = 100;
    public float Opacity = 1;
    // Fractions of the available viewport space keep the gem on screen after resizing.
    public float PositionX = 0.55f;
    public float PositionY = 0.65f;

    public void Normalize()
    {
        Size = GemState.ClampFinite(Size, 40, 300, 100);
        Opacity = GemState.ClampFinite(Opacity, 0.15f, 1, 1);
        PositionX = GemState.ClampFinite(PositionX, 0, 1, 0.55f);
        PositionY = GemState.ClampFinite(PositionY, 0, 1, 0.65f);
    }
}
