using Content.Shared.ADT.DrunkDrift;
using Content.Shared.Drunk;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Server.ADT.DrunkDrift;

/// <summary>ADT: маркер пьяного и осмотр нетрезвых.</summary>
public sealed class DrunkDriftSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<MobStateComponent> _mobQuery;

    private float _accumulator;

    public override void Initialize()
    {
        _mobQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectAppliedEvent>(OnDrunkApplied);
        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectRemovedEvent>(OnDrunkRemoved);
        SubscribeLocalEvent<ADTDrunkDriftComponent, ExaminedEvent>(OnExamined);
    }

    private void OnDrunkApplied(Entity<DrunkStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        var comp = EnsureComp<ADTDrunkDriftComponent>(args.Target);
        UpdateVisuals(args.Target, comp);
    }

    private void OnDrunkRemoved(Entity<DrunkStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        // Держим маркер, пока есть хоть один эффект опьянения.
        if (_statusEffects.HasEffectComp<DrunkStatusEffectComponent>(args.Target))
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
        if (_statusEffects.TryGetMaxTime<DrunkStatusEffectComponent>(uid, out var time))
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

    private void OnExamined(Entity<ADTDrunkDriftComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.VisualsActive)
            return;

        if (!_mobQuery.TryComp(ent.Owner, out var mob) || mob.CurrentState != MobState.Alive)
            return;

        args.PushMarkup(Loc.GetString("adt-drunk-examine", ("ent", ent.Owner)));
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
}
