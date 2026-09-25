using System.Numerics;
using Content.Shared._RMC14.Stealth;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Medical;

public sealed partial class ADTHealingVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    private const float StealthVisibilityThreshold = 0f;

    public bool TryStartHealEffect(EntityUid target, EntProtoId? effect)
    {
        if (effect is not { } proto || IsHiddenByStealth(target))
            return false;

        var visuals = EnsureComp<ADTHealingVisualsComponent>(target);
        if (Exists(visuals.ActiveEffect))
            return false;

        visuals.ActiveEffect = PredictedSpawnAttachedTo(proto, new EntityCoordinates(target, Vector2.Zero));
        return true;
    }

    public void StopHealEffect(EntityUid target)
    {
        if (!TryComp<ADTHealingVisualsComponent>(target, out var visuals))
            return;

        if (!Exists(visuals.ActiveEffect))
            return;

        PredictedQueueDel(visuals.ActiveEffect.Value);
        visuals.ActiveEffect = null;
    }

    private bool IsHiddenByStealth(EntityUid target)
    {
        var uid = target;
        while (uid.IsValid())
        {
            if (HasComp<EntityActiveInvisibleComponent>(uid))
                return true;

            if (TryComp<StealthComponent>(uid, out var stealth)
                && stealth.Enabled
                && _stealth.GetVisibility(uid, stealth) <= StealthVisibilityThreshold)
            {
                return true;
            }

            uid = Transform(uid).ParentUid;
        }

        return false;
    }
}