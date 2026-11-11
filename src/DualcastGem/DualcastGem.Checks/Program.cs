using DualcastGem;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}

Check(GemState.Resolve(new uint[] { 50, 1249 }) == GemMode.Dualcast, "Dualcast is red");
Check(GemState.Resolve(new uint[] { 167 }) == GemMode.Swiftcast, "Swiftcast alone is blue");
Check(GemState.Resolve(new uint[] { 1249, 167 }) == GemMode.Swiftcast, "Swiftcast overrides Dualcast");
Check(GemState.Resolve(new uint[] { 167, 1249 }) == GemMode.Swiftcast, "Priority ignores status ordering");
Check(GemState.Resolve(Array.Empty<uint>()) == GemMode.Inactive, "Neither buff is gray");
Check(GemState.Resolve(new uint[] { 1211 }) == GemMode.Inactive, "Unrelated effects do not light gem");
// A fresh snapshot must immediately reflect consumption/expiry without retaining the prior color.
var snapshots = new uint[][] { new uint[] { 1249 }, new uint[] { 1249, 167 }, new uint[] { 1249 }, new uint[] { 167 }, Array.Empty<uint>() };
var expectedModes = new[] { GemMode.Dualcast, GemMode.Swiftcast, GemMode.Dualcast, GemMode.Swiftcast, GemMode.Inactive };
for (var i = 0; i < snapshots.Length; i++)
    Check(GemState.Resolve(snapshots[i]) == expectedModes[i], $"Buff transition {i}");
foreach (var mode in new[] { GemMode.Dualcast, GemMode.Swiftcast })
    Check(GemState.IsVisible(true, true, 35, true, false, true, mode != GemMode.Inactive, false), $"Hide-inactive keeps {mode} visible");
// Exercise every combination of visibility gates, including stale status on job changes/logout.
for (var bits = 0; bits < 256; bits++)
{
    bool Bit(int index) => (bits & (1 << index)) != 0;
    var enabled = Bit(0);
    var loggedIn = Bit(1);
    var redMage = Bit(2);
    var inCombat = Bit(3);
    var combatOnly = Bit(4);
    var hideInactive = Bit(5);
    var active = Bit(6);
    var blocked = Bit(7);
    var expected = true;
    if (!enabled || !loggedIn || !redMage || blocked) expected = false;
    if (combatOnly && !inCombat) expected = false;
    if (hideInactive && !active) expected = false;
    Check(GemState.IsVisible(enabled, loggedIn, redMage ? 35u : 32u, inCombat,
        combatOnly, hideInactive, active, blocked) == expected, $"Visibility case {bits}");
}
Check(GemState.ClampFinite(float.NaN, 40, 300, 100) == 100, "Invalid saved size resets");
Check(GemState.ClampFinite(float.PositiveInfinity, 0, 1, 0.55f) == 0.55f, "Invalid position resets");
Check(GemState.ClampFinite(-5, 0, 1, 0.55f) == 0, "Position cannot move past left edge");
Check(GemState.ClampFinite(10, 0, 1, 0.55f) == 1, "Position cannot move past right edge");
Console.WriteLine($"Passed {checks} Dualcast Gem checks.");
