using Content.Shared.Actions;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.EntitySystems;
using Robust.Shared.Network;

namespace Content.Shared.ADT.Mech.EntitySystems;

public sealed class MechEquipmentActionsSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechOverloadComponent, SetupMechUserEvent>(SetupOverloadUser);
        SubscribeLocalEvent<MechPhazeComponent, SetupMechUserEvent>(SetupPhaseUser);
        SubscribeLocalEvent<MechPhazeComponent, ComponentStartup>(StartupPhaze);
        SubscribeLocalEvent<MechThrusterComponent, SetupMechUserEvent>(SetupThrusterUser);
    }

    private void SetupThrusterUser(Entity<MechThrusterComponent> ent, ref SetupMechUserEvent args)
    {
        if (_net.IsServer)
            _actions.AddAction(args.Pilot, ref ent.Comp.MechThrusterActionEntity, ent.Comp.MechThrusterAction, ent);
    }

    private void SetupOverloadUser(Entity<MechOverloadComponent> ent, ref SetupMechUserEvent args)
    {
        if (!_net.IsServer)
            return;

        _actions.AddAction(args.Pilot, ref ent.Comp.MechOverloadActionEntity, ent.Comp.MechOverloadAction, ent);
        _actions.SetToggled(ent.Comp.MechOverloadActionEntity, ent.Comp.Overload);
        Dirty(ent);
    }

    private void SetupPhaseUser(Entity<MechPhazeComponent> ent, ref SetupMechUserEvent args)
    {
        if (_net.IsServer)
            _actions.AddAction(args.Pilot, ref ent.Comp.MechPhazeActionEntity, ent.Comp.MechPhazeAction, ent);
    }

    private void StartupPhaze(Entity<MechPhazeComponent> ent, ref ComponentStartup args)
    {
        _appearance.SetData(ent, MechPhazingVisuals.Phazing, ent.Comp.Phazed);
    }
}
