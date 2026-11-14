using Content.Shared.ADT.Heretic.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

public abstract partial class SharedXRayVisionSystem : EntitySystem
{
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XRayVisionStatusEffectComponent, StatusEffectAppliedEvent>(OnApply);
        SubscribeLocalEvent<XRayVisionStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);
    }

    private void OnRemove(Entity<XRayVisionStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || !TryComp(args.Target, out EyeComponent? eye))
            return;

        if (!_status.TryEffectsWithComp<XRayVisionStatusEffectComponent>(args.Target, out var effects) ||
            effects.Count == 0)
        {
            _eye.SetDrawFov(args.Target, ent.Comp.ShouldRemoveFov, eye);
            DrawLight(args.Target, ent.Comp.ShouldRemoveFov);
        }
        else
        {
            _eye.SetDrawFov(args.Target, false, eye);
            if (!ent.Comp.ShouldRemoveFov)
                return;
            foreach (var (_, comp, _) in effects)
            {
                comp.ShouldRemoveFov = true;
            }
        }
    }

    private void OnApply(Entity<XRayVisionStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!TryComp(args.Target, out EyeComponent? eye))
            return;

        if (_status.TryEffectsWithComp<XRayVisionStatusEffectComponent>(args.Target, out var effects))
            effects.RemoveWhere(x => x.Owner == ent.Owner);

        if (_timing.IsFirstTimePredicted)
            ent.Comp.ShouldRemoveFov = eye.DrawFov && effects is not { Count: > 0 };

        _eye.SetDrawFov(args.Target, false, eye);
        DrawLight(args.Target, false);
    }

    protected virtual void DrawLight(EntityUid uid, bool value) { }
}
