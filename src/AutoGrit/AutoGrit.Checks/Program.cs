using AutoGrit;

var passed = 0;
void Check(string name, Action test)
{
    test();
    passed++;
    Console.WriteLine($"PASS {name}");
}
void Expect(bool result, bool expected)
{
    if (result != expected) throw new Exception($"Expected {expected}, received {result}");
}
CombatStartController Ready()
{
    var c = new CombatStartController();
    Expect(c.ShouldAttempt(0, false, true, false), false);
    return c;
}
Check("Missing Grit triggers on combat entry", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false), true);
});
Check("Existing Grit is never toggled, even if removed later", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, true), false);
    Expect(c.ShouldAttempt(1000, true, true, false), false);
});
Check("Accepted action is not repeated before status arrives", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false), true);
    c.ActionAccepted();
    Expect(c.ShouldAttempt(501, true, true, false), false);
    Expect(c.ShouldAttempt(1001, true, true, false), false);
});
Check("Unavailable action retries at bounded intervals and expires", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false), true);
    Expect(c.ShouldAttempt(500, true, true, false), false);
    Expect(c.ShouldAttempt(501, true, true, false), true);
    Expect(c.ShouldAttempt(5001, true, true, false), false);
    Expect(c.ShouldAttempt(6000, true, true, false), false);
});
Check("Grit applied manually cancels pending retry", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false), true);
    Expect(c.ShouldAttempt(2, true, true, true), false);
    Expect(c.ShouldAttempt(1000, true, true, false), false);
});
Check("Disabled or ineligible players do not activate mid-combat", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, false, false), false);
    Expect(c.ShouldAttempt(501, true, true, false), false);
});
Check("Becoming ineligible cancels pending action", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false), true);
    Expect(c.ShouldAttempt(2, true, false, false), false);
    Expect(c.ShouldAttempt(501, true, true, false), false);
});
Check("Next combat can activate again", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false), true);
    c.ActionAccepted();
    Expect(c.ShouldAttempt(2, false, true, false), false);
    Expect(c.ShouldAttempt(3, true, true, false), true);
});
Check("Loading plugin mid-combat does not activate", () =>
{
    var c = new CombatStartController();
    Expect(c.ShouldAttempt(1, true, true, false), false);
    Expect(c.ShouldAttempt(501, true, true, false), false);
});
Check("Leaving combat cancels pending retries", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false), true);
    Expect(c.ShouldAttempt(2, false, true, false), false);
    Expect(c.ShouldAttempt(501, false, true, false), false);
});
Check("Logout reset does not treat existing combat as new", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false), true);
    c.Reset();
    Expect(c.ShouldAttempt(501, true, true, false), false);
});
Check("Every tank maps to its own stance; other jobs do not", () =>
{
    foreach (var (job, action, status) in new (uint, uint, uint)[]
        { (1,28,79), (19,28,79), (3,48,91), (21,48,91), (32,3629,743), (37,16142,1833) })
    {
        var stance = TankStance.ForJob(job);
        Expect(stance?.ActionId == action && stance?.StatusId == status, true);
    }
    Expect(TankStance.ForJob(24).HasValue, false);
});
Check("Dungeon entry triggers without combat", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, false, true, false, 100), true);
    c.ActionAccepted();
    Expect(c.ShouldAttempt(501, false, true, false, 100), false);
});
Check("Dungeon entry with existing stance never toggles", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, false, true, true, 100), false);
    Expect(c.ShouldAttempt(501, false, true, false, 100), false);
});
Check("Dungeon entry waits through loading or cutscene", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, false, true, false, 100, false), false);
    Expect(c.ShouldAttempt(10000, false, true, false, 100, false), false);
    Expect(c.ShouldAttempt(10001, false, true, false, 100, true), true);
});
Check("Dungeon retry expires and leaving cancels it", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, false, true, false, 100, false), false);
    Expect(c.ShouldAttempt(120001, false, true, false, 100), false);
    Expect(c.ShouldAttempt(120501, false, true, false, 100), false);
    c = Ready();
    Expect(c.ShouldAttempt(1, false, true, false, 100, false), false);
    Expect(c.ShouldAttempt(2, false, true, false, 0), false);
});
Check("Joining an existing dungeon checks stance immediately", () =>
{
    var c = new CombatStartController();
    Expect(c.ShouldAttempt(1, false, true, false, 100), true);
});
Check("Moving to another dungeon triggers a new check", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, false, true, false, 100), true);
    c.ActionAccepted();
    Expect(c.ShouldAttempt(1000, false, true, false, 200), true);
});
Check("Dungeon loading does not mistake pre-sync stance for completed check", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, false, true, true, 100, false), false);
    Expect(c.ShouldAttempt(1000, false, true, false, 100, true), true);
});
Check("Combat trigger can be disabled independently", () =>
{
    var c = Ready();
    var o = new ActivationOptions { CombatEnabled = false };
    Expect(c.ShouldAttempt(1, true, true, false, options: o), false);
    Expect(c.ShouldAttempt(2, false, true, false, 100, options: o), true);
});
Check("Duty entry trigger can be disabled independently", () =>
{
    var c = Ready();
    var o = new ActivationOptions { DutyEntryEnabled = false };
    Expect(c.ShouldAttempt(1, false, true, false, 100, options: o), false);
    Expect(c.ShouldAttempt(2, true, true, false, 100, options: o), true);
});
Check("Combat delay precedes retry window", () =>
{
    var c = Ready();
    var o = new ActivationOptions { CombatDelayMs = 10000, RetryDurationMs = 2000 };
    Expect(c.ShouldAttempt(1, true, true, false, options: o), false);
    Expect(c.ShouldAttempt(10000, true, true, false, options: o), false);
    Expect(c.ShouldAttempt(10001, true, true, false, options: o), true);
    Expect(c.ShouldAttempt(10501, true, true, false, options: o), true);
    Expect(c.ShouldAttempt(12001, true, true, false, options: o), false);
});
Check("Other tank delay applies only in duties", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false, anotherTankHasStance: true), true);
    c = Ready();
    Expect(c.ShouldAttempt(1, true, true, false, 100, anotherTankHasStance: true), false);
    Expect(c.ShouldAttempt(10000, true, true, false, 100, anotherTankHasStance: true), false);
    Expect(c.ShouldAttempt(10001, true, true, false, 100, anotherTankHasStance: true), true);
});
Check("Duty entry cannot bypass other tank head start", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, false, true, false, 100, anotherTankHasStance: true), false);
    Expect(c.ShouldAttempt(1000, false, true, false, 100, anotherTankHasStance: true), false);
    Expect(c.ShouldAttempt(2000, true, true, false, 100, anotherTankHasStance: true), false);
    Expect(c.ShouldAttempt(12000, true, true, false, 100, anotherTankHasStance: true), true);
});
Check("Other tank option can be switched off", () =>
{
    var c = Ready();
    var o = new ActivationOptions { DelayForOtherTank = false };
    Expect(c.ShouldAttempt(1, true, true, false, 100, options: o, anotherTankHasStance: true), true);
});
Check("Other tank gaining stance during a pending retry extends delay once", () =>
{
    var c = Ready();
    var o = new ActivationOptions { DutyEntryEnabled = false };
    Expect(c.ShouldAttempt(1, true, true, false, 100, options: o), true);
    Expect(c.ShouldAttempt(501, true, true, false, 100, options: o, anotherTankHasStance: true), false);
    Expect(c.ShouldAttempt(10001, true, true, false, 100, options: o, anotherTankHasStance: true), true);
    Expect(c.ShouldAttempt(15001, true, true, false, 100, options: o, anotherTankHasStance: true), false);
});
Check("Longer general delay wins over other tank delay", () =>
{
    var c = Ready();
    var o = new ActivationOptions { CombatDelayMs = 20000 };
    Expect(c.ShouldAttempt(1, true, true, false, 100, options: o, anotherTankHasStance: true), false);
    Expect(c.ShouldAttempt(10001, true, true, false, 100, options: o, anotherTankHasStance: true), false);
    Expect(c.ShouldAttempt(20001, true, true, false, 100, options: o, anotherTankHasStance: true), true);
});
Check("Existing stance is never turned off for another tank", () =>
{
    var c = Ready();
    Expect(c.ShouldAttempt(1, true, true, true, 100, anotherTankHasStance: true), false);
    Expect(c.ShouldAttempt(10001, true, true, true, 100, anotherTankHasStance: true), false);
});
Check("Disabling a trigger cancels its pending delayed action", () =>
{
    var c = Ready();
    var o = new ActivationOptions { CombatDelayMs = 1000 };
    Expect(c.ShouldAttempt(1, true, true, false, options: o), false);
    Expect(c.ShouldAttempt(1001, true, true, false, options: o with { CombatEnabled = false }), false);
    Expect(c.ShouldAttempt(1501, true, true, false, options: o), false);
});
Console.WriteLine($"{passed} checks passed.");
