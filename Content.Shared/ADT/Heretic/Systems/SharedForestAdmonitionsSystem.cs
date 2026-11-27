using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

public abstract partial class SharedForestAdmonitionsSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedTransformSystem XForm = default!;

    [Dependency] private SharedShadowCloakSystem _cloak = default!;
    [Dependency] private TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForestAdmonitionsComponent, SelfBeforeGunShotEvent>(OnShot);
        SubscribeLocalEvent<ForestAdmonitionsEntityComponent, ExamineAttemptEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<ForestAdmonitionsEntityComponent> ent, ref ExamineAttemptEvent args)
    {
        if (CalculateVisibilityFactor(ent, args.Examiner) < ent.Comp.ExamineThreshold)
            args.Cancel();
    }

    private void OnShot(Entity<ForestAdmonitionsComponent> ent, ref SelfBeforeGunShotEvent args)
    {
        RevealCloak(ent.AsNullable());
    }

    private void RevealCloak(Entity<ForestAdmonitionsComponent?, ShadowCloakedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        if (_cloak.GetShadowCloakEntity(ent) is not { } cloak ||
            !TryComp(cloak, out ForestAdmonitionsEntityComponent? comp))
            return;

        comp.LastRevealTime = Timing.CurTime;
        comp.NextUpdate = comp.LastRevealTime;
        Dirty(cloak, comp);
    }

    protected float CalculateVisibilityFactor(Entity<ForestAdmonitionsEntityComponent> ent, EntityUid viewer)
    {
        var diff = (float) (Timing.CurTime.TotalSeconds - ent.Comp.LastRevealTime.TotalSeconds);
        var factor = ent.Comp.RevealDuration > 0f ? Math.Clamp(1f - diff / ent.Comp.RevealDuration, 0f, 1f) : 0f;
        if (ent.Owner == viewer)
            return factor == 0f ? ent.Comp.SelfVisibility : 1f;

        var us = XForm.GetMapCoordinates(ent);
        var them = XForm.GetMapCoordinates(viewer);

        if (us.MapId != them.MapId)
            return 0f;

        var distance = (us.Position - them.Position).Length();
        var soft = ent.Comp.RevealDistanceSoft;
        factor += Math.Clamp(1f - (distance - soft) / Math.Max(ent.Comp.RevealDistance - soft, 0.1f), 0f, 1f);
        return Math.Clamp(factor, 0f, 1f);
    }
}
