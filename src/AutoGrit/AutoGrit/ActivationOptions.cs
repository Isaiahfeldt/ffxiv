namespace AutoGrit;

internal sealed record ActivationOptions
{
    public bool CombatEnabled { get; init; } = true;
    public bool DutyEntryEnabled { get; init; } = true;
    public int CombatDelayMs { get; init; }
    public int RetryDurationMs { get; init; } = 5000;
    public bool DelayForOtherTank { get; init; } = true;
    public int OtherTankDelayMs { get; init; } = 10000;
}
