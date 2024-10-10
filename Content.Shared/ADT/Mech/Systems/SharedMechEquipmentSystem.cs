using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Robust.Shared.Random;
using Content.Shared.Overlays;
using Content.Shared.Whitelist;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.EntitySystems;

namespace Content.Shared.ADT.Mech.EntitySystems;

public abstract class SharedMechEquipmentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MechOverloadComponent, SetupMechUserEvent>(SetupOverloadUser);
        SubscribeLocalEvent<MechPhazeComponent, SetupMechUserEvent>(SetupPhaseUser);
        SubscribeLocalEvent<MechPhazeComponent, ComponentStartup>(StartupPhaze);
    }

    private void SetupOverloadUser(EntityUid uid, MechOverloadComponent comp, ref SetupMechUserEvent args)
    {
        var pilot = args.Pilot;
        _actions.AddAction(pilot, ref comp.MechOverloadActionEntity, comp.MechOverloadAction, uid);
    }

    private void SetupPhaseUser(EntityUid uid, MechPhazeComponent comp, ref SetupMechUserEvent args)
    {
        var pilot = args.Pilot;
        _actions.AddAction(pilot, ref comp.MechPhazeActionEntity, comp.MechPhazeAction, uid);
    }

    private void StartupPhaze(EntityUid uid, MechPhazeComponent comp, ComponentStartup args)
    {
        _appearance.SetData(uid, MechPhazingVisuals.Phazing, false);
    }
}
