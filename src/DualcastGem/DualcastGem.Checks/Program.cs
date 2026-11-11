using DualcastGem;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}

Check(GemState.IsActive(new uint[] { 50, 1249 }), "Dualcast lights the gem");
Check(!GemState.IsActive(new uint[] { 167 }), "Swiftcast alone stays unlit");
Check(!GemState.IsActive(Array.Empty<uint>()), "Consumed/expired status stays unlit");
Check(!GemState.IsActive(new uint[] { 1211 }), "Other instant-cast effects stay unlit");
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
