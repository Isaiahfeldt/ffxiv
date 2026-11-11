using System;
using System.Collections.Generic;
using System.Linq;

namespace DualcastGem;

internal static class GemState
{
    internal const uint RedMageJob = 35;
    internal const uint DualcastStatus = 1249;

    internal static bool IsActive(IEnumerable<uint> statuses) => statuses.Contains(DualcastStatus);

    internal static bool IsVisible(bool enabled, bool loggedIn, uint job, bool inCombat,
        bool combatOnly, bool hideInactive, bool active, bool blocked) =>
        enabled && loggedIn && job == RedMageJob && !blocked
        && (!combatOnly || inCombat) && (!hideInactive || active);

    internal static float ClampFinite(float value, float min, float max, float fallback) =>
        Math.Clamp(float.IsFinite(value) ? value : fallback, min, max);
}
