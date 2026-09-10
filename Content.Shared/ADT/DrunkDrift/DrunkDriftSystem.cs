using Content.Shared.Drunk;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.DrunkDrift;

public sealed partial class DrunkDriftSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _timing = default!;

    private float _accumulator;
    private EntityQuery<MobStateComponent> _mobQuery;

    public override void Initialize()
    {
        base.Initialize();

        _mobQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<StatusEffectComponent, StatusEffectAppliedEvent>(OnDrunkApplied);
        SubscribeLocalEvent<StatusEffectComponent, StatusEffectRemovedEvent>(OnDrunkRemoved);
        SubscribeLocalEvent<ADTDrunkDriftComponent, ExaminedEvent>(OnExamined);
    }

    private void OnDrunkApplied(Entity<StatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        // Bloodloss and woozy effects also carry DrunkStatusEffect - wobble only from alcohol.
        if (MetaData(ent.Owner).EntityPrototype?.ID != SharedDrunkSystem.Drunk.Id)
            return;

        if (_timing.ApplyingState)
            return;

        var comp = EnsureComp<ADTDrunkDriftComponent>(args.Target);
        UpdateVisuals(args.Target, comp);
    }

    private void OnDrunkRemoved(Entity<StatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (MetaData(ent.Owner).EntityPrototype?.ID != SharedDrunkSystem.Drunk.Id)
            return;

        if (_timing.ApplyingState)
            return;

        // Keep the marker while any drunkenness effect remains.
        if (_statusEffects.HasStatusEffect(args.Target, SharedDrunkSystem.Drunk))
        {
            if (TryComp<ADTDrunkDriftComponent>(args.Target, out var comp))
                UpdateVisuals(args.Target, comp);

            return;
        }

        RemComp<ADTDrunkDriftComponent>(args.Target);
    }

    private void UpdateVisuals(EntityUid uid, ADTDrunkDriftComponent comp)
    {
        var active = false;
        if (_statusEffects.TryGetTime(uid, SharedDrunkSystem.Drunk, out var time))
        {
            var remaining = time.EndEffectTime - _timing.CurTime;
            active = remaining == null
                || remaining.Value >= comp.VisualThreshold;
        }

        if (comp.VisualsActive == active)
            return;

        comp.VisualsActive = active;
        Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator < 1f)
            return;

        _accumulator -= 1f;

        var query = EntityQueryEnumerator<ADTDrunkDriftComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateVisuals(uid, comp);
        }
    }

    private void OnExamined(Entity<ADTDrunkDriftComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.VisualsActive)
            return;

        if (!_mobQuery.TryComp(ent.Owner, out var mob) || mob.CurrentState != MobState.Alive)
            return;

        args.PushMarkup(Loc.GetString("adt-drunk-examine", ("ent", ent.Owner)));
    }
}
