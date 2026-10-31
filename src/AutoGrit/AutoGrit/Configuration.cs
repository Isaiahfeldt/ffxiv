using System;
using Dalamud.Configuration;

namespace AutoGrit;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;
    public bool Enabled { get; set; } = true;
    public bool EnableOnCombatStart { get; set; } = true;
    public bool EnableOnDutyEntry { get; set; } = true;
    public bool EnablePaladin { get; set; } = true;
    public bool EnableWarrior { get; set; } = true;
    public bool EnableDarkKnight { get; set; } = true;
    public bool EnableGunbreaker { get; set; } = true;
    public bool DelayWhenOtherTankHasStance { get; set; } = true;
    public float CombatDelaySeconds { get; set; }
    public float OtherTankDelaySeconds { get; set; } = 10;
    public float RetryDurationSeconds { get; set; } = 5;
    public bool ShowActivationMessage { get; set; }

    public bool IsJobEnabled(uint job) => job switch
    {
        1 or 19 => EnablePaladin,
        3 or 21 => EnableWarrior,
        32 => EnableDarkKnight,
        37 => EnableGunbreaker,
        _ => false,
    };

    internal ActivationOptions GetOptions() => new()
    {
        CombatEnabled = EnableOnCombatStart,
        DutyEntryEnabled = EnableOnDutyEntry,
        CombatDelayMs = Milliseconds(CombatDelaySeconds, 60),
        OtherTankDelayMs = Milliseconds(OtherTankDelaySeconds, 60),
        RetryDurationMs = Math.Max(1000, Milliseconds(RetryDurationSeconds, 30)),
        DelayForOtherTank = DelayWhenOtherTankHasStance,
    };

    private static int Milliseconds(float seconds, int maximum) =>
        (int)(Math.Clamp(float.IsFinite(seconds) ? seconds : 0, 0, maximum) * 1000);
}
