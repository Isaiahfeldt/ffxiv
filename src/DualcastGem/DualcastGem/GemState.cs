using System;
using System.Collections.Generic;

namespace DualcastGem;

internal enum GemMode { Inactive, Dualcast, Swiftcast }

internal static class GemState
{
    internal const uint RedMageJob = 35;
    internal const uint DualcastStatus = 1249;

    internal const uint SwiftcastStatus = 167;

    internal static GemMode Resolve(IEnumerable<uint> statuses)
    {
        var result = GemMode.Inactive;
        foreach (var status in statuses)
        {
            if (status == SwiftcastStatus) return GemMode.Swiftcast;
            if (status == DualcastStatus) result = GemMode.Dualcast;
        }
        return result;
    }

    internal static bool IsVisible(bool enabled, bool loggedIn, uint job, bool inCombat,
        bool combatOnly, bool hideInactive, bool active, bool blocked) =>
        enabled && loggedIn && job == RedMageJob && !blocked
        && (!combatOnly || inCombat) && (!hideInactive || active);

    internal static float ClampFinite(float value, float min, float max, float fallback) =>
        Math.Clamp(float.IsFinite(value) ? value : fallback, min, max);
}
