using Content.Shared.ADT.Heretic.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class CrucibleSoulStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId AlertEffect = "StatusEffectCrucibleSoul";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrucibleSoulStatusEffectComponent, StatusEffectAppliedEvent>(OnApply);
        SubscribeLocalEvent<CrucibleSoulStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);
        SubscribeLocalEvent<ToggleCrucibleSoulEvent>(OnToggleCrucibleSoul);
    }

    private void OnToggleCrucibleSoul(ToggleCrucibleSoulEvent args)
    {
        args.Handled = _status.TryRemoveStatusEffect(args.User, AlertEffect);
    }

    private void OnRemove(Entity<CrucibleSoulStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.Coords is not { } coords || TerminatingOrDeleted(args.Target))
            return;

        _transform.SetCoordinates(args.Target, coords);
    }

    private void OnApply(Entity<CrucibleSoulStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Coords = Transform(args.Target).Coordinates;
    }
}
