namespace AutoGrit;

internal readonly record struct TankStance(uint ActionId, uint StatusId, string Name)
{
    public static TankStance? ForJob(uint jobId) => jobId switch
    {
        1 or 19 => new(28, 79, "Iron Will"),       // Gladiator / Paladin
        3 or 21 => new(48, 91, "Defiance"),       // Marauder / Warrior
        32 => new(3629, 743, "Grit"),             // Dark Knight
        37 => new(16142, 1833, "Royal Guard"),    // Gunbreaker
        _ => null,
    };
}
