using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.ADT.Implants;

public sealed class ImplantCritThresholdSystem : EntitySystem
{
    private static readonly MobState[] CritStates = { MobState.SoftCritical, MobState.Critical };

    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public bool TryGetFirstCritThreshold(EntityUid uid, out FixedPoint2 threshold)
    {
        var found = false;
        threshold = FixedPoint2.MaxValue;

        foreach (var state in CritStates)
        {
            if (!_mobThreshold.TryGetThresholdForState(uid, state, out var value))
                continue;

            found = true;
            threshold = FixedPoint2.Min(threshold, value.Value);
        }

        return found;
    }
}
