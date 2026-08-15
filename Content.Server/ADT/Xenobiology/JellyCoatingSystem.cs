using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Xenobiology;

[Virtual]
public partial class JellyCoatingSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JellyCoatingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SpeedBoostedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<MechPilotComponent, RefreshMovementSpeedModifiersEvent>(OnPilotRefreshSpeed);
    }

    private void OnAfterInteract(Entity<JellyCoatingComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.Target.HasValue)
            return;

        var target = args.Target.Value;
        if (HasComp<HumanoidProfileComponent>(target))
            return;

        if (!TryComp<MovementSpeedModifierComponent>(target, out var _))
            return;

        if (HasComp<SpeedBoostedComponent>(target))
            return;

        var boosted = EnsureComp<SpeedBoostedComponent>(target);
        boosted.SpeedMultiplier = ent.Comp.SpeedMultiplier;

        _popup.PopupEntity(Loc.GetString("jelly-coating-success", ("target", target), ("source", ent.Owner)), target);
        Dirty(target, boosted);

        _entityManager.PredictedQueueDeleteEntity(ent.Owner);
        args.Handled = true;
    }

    private void OnRefreshSpeed(EntityUid uid, SpeedBoostedComponent component, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.SpeedMultiplier);
    }

    private void OnPilotRefreshSpeed(EntityUid uid, MechPilotComponent pilot, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!HasComp<MechComponent>(pilot.Mech))
            return;

        if (!TryComp<SpeedBoostedComponent>(pilot.Mech, out var boosted))
            return;

        args.ModifySpeed(boosted.SpeedMultiplier);
    }
}
