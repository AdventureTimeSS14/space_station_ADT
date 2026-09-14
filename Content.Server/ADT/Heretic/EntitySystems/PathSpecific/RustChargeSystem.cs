using Content.Server.Destructible;
using Content.Shared.ADT.Heretic.Systems;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed class RustChargeSystem : SharedRustChargeSystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;

    protected override void DestroyStructure(EntityUid uid, EntityUid user)
    {
        base.DestroyStructure(uid, user);

        if (!TryComp(uid, out DestructibleComponent? destructible) || destructible.Thresholds.Count == 0)
        {
            Del(uid);
            return;
        }

        var threshold = destructible.Thresholds[^1];
        RaiseLocalEvent(uid, new DamageThresholdReached(destructible, threshold), true);

        // ADT: DamageThreshold has no Execute, run behaviors directly
        foreach (var behavior in threshold.Behaviors)
        {
            behavior.Execute(uid, _destructible, user);
        }
    }
}
