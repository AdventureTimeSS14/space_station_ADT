using Content.Shared.ADT.DrunkDrift;
using Content.Shared.Drunk;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Server.ADT.DrunkDrift;

/// <summary>ADT: маркер пьяного и обновление визуального состояния.</summary>
public sealed class DrunkDriftSystem : SharedDrunkDriftSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectAppliedEvent>(OnDrunkApplied);
        SubscribeLocalEvent<DrunkStatusEffectComponent, StatusEffectRemovedEvent>(OnDrunkRemoved);
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
