using System;

namespace AutoGrit;

// All decisions use a monotonic clock supplied by the caller.
internal sealed class CombatStartController
{
    internal const long RetryIntervalMs = 500;
    private bool observed;
    private bool wasInCombat;
    private uint previousDuty;
    private bool pending;
    private bool pendingDuty;
    private bool extendedForOtherTank;
    private long combatStartedAt;
    private long notBefore;
    private long deadline;
    private long nextAttempt;

    public bool ShouldAttempt(long now, bool inCombat, bool eligible, bool hasStance,
        uint dutyTerritory = 0, bool canAct = true, ActivationOptions? options = null,
        bool anotherTankHasStance = false)
    {
        options ??= new ActivationOptions();
        var enteredCombat = observed && !wasInCombat && inCombat;
        var enteredDuty = dutyTerritory != 0 && dutyTerritory != previousDuty;
        observed = true;
        wasInCombat = inCombat;
        previousDuty = dutyTerritory;
        if (enteredCombat)
            combatStartedAt = now;

        if (!eligible || (hasStance && canAct))
        {
            Cancel();
            return false;
        }

        var deferToOtherTank = dutyTerritory != 0 && options.DelayForOtherTank && anotherTankHasStance;
        if (enteredDuty && options.DutyEntryEnabled)
        {
            pending = true;
            pendingDuty = true;
            extendedForOtherTank = false;
            notBefore = now;
            deadline = now + 120000; // Allow loading/cutscenes to finish.
            nextAttempt = now;
        }
        if (enteredCombat && options.CombatEnabled && (!pendingDuty || deferToOtherTank))
        {
            pending = true;
            pendingDuty = false;
            extendedForOtherTank = deferToOtherTank;
            var delay = Math.Max(0, options.CombatDelayMs);
            if (deferToOtherTank)
                delay = Math.Max(delay, options.OtherTankDelayMs);
            notBefore = now + delay;
            // Retry time starts after the chosen delay, so a long delay cannot expire it.
            deadline = notBefore + Math.Max(1, options.RetryDurationMs);
            nextAttempt = notBefore;
        }

        if (!pending)
            return false;
        if ((pendingDuty && (!options.DutyEntryEnabled || dutyTerritory == 0))
            || (!pendingDuty && (!options.CombatEnabled || !inCombat)))
        {
            Cancel();
            return false;
        }
        if (pendingDuty && deferToOtherTank)
        {
            // Do not let duty-entry activation bypass the combat-start head start.
            Cancel();
            return false;
        }
        if (!pendingDuty && deferToOtherTank && !extendedForOtherTank)
        {
            extendedForOtherTank = true;
            notBefore = Math.Max(notBefore, combatStartedAt + options.OtherTankDelayMs);
            deadline = notBefore + Math.Max(1, options.RetryDurationMs);
            nextAttempt = Math.Max(nextAttempt, notBefore);
        }
        if (now >= deadline)
        {
            Cancel();
            return false;
        }
        if (!canAct || now < nextAttempt || now < notBefore)
            return false;
        if (hasStance)
        {
            Cancel();
            return false;
        }

        nextAttempt = now + RetryIntervalMs;
        return true;
    }

    public void ActionAccepted() => Cancel();

    private void Cancel()
    {
        pending = false;
        pendingDuty = false;
        extendedForOtherTank = false;
    }

    public void Reset()
    {
        observed = false;
        wasInCombat = false;
        previousDuty = 0;
        Cancel();
    }
}
