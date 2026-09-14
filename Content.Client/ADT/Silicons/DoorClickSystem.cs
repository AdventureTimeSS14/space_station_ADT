using Content.Shared.ADT.Silicons;
using Content.Shared.Doors.Components;
using Content.Shared.Electrocution;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client.ADT.Silicons;
public sealed class DoorClickSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ADTDoorEmergencyAccess, new PointerInputCmdHandler(OnSpaceClick, outsidePrediction: true))
            .BindBefore(ContentKeyFunctions.TryPullObject, new PointerInputCmdHandler(OnCtrlClick, outsidePrediction: true), typeof(SharedInteractionSystem))
            .BindBefore(ContentKeyFunctions.AltActivateItemInWorld, new PointerInputCmdHandler(OnAltClick, outsidePrediction: true), typeof(SharedInteractionSystem))
            .Register<DoorClickSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<DoorClickSystem>();
        base.Shutdown();
    }

    private bool OnSpaceClick(in PointerInputCmdArgs args) => TryDoorClick(args, DoorClickAction.EmergencyAccess);

    private bool OnCtrlClick(in PointerInputCmdArgs args) => TryDoorClick(args, DoorClickAction.Bolt);

    private bool OnAltClick(in PointerInputCmdArgs args) => TryDoorClick(args, DoorClickAction.Electrify);

    private bool TryDoorClick(in PointerInputCmdArgs args, DoorClickAction action)
    {
        if (!args.EntityUid.IsValid() || !Exists(args.EntityUid))
            return false;

        if (_player.LocalEntity is not { } player ||
            (!HasComp<StationAiHeldComponent>(player) && !HasComp<BorgChassisComponent>(player)))
            return false;

        if (!HasComp<DoorComponent>(args.EntityUid))
            return false;

        RaiseNetworkEvent(new DoorClickEvent(GetNetEntity(args.EntityUid), action));

        switch (action)
        {
            case DoorClickAction.EmergencyAccess:
                RaiseLocalEvent(args.EntityUid,
                    new StationAiEmergencyAccessEvent
                    {
                        User = player,
                        EmergencyAccess = !TryComp(args.EntityUid, out AirlockComponent? airlock) || !airlock.EmergencyAccess,
                    });
                break;
            case DoorClickAction.Bolt:
                RaiseLocalEvent(args.EntityUid,
                    new StationAiBoltEvent
                    {
                        User = player,
                        Bolted = !TryComp(args.EntityUid, out DoorBoltComponent? bolt) || !bolt.BoltsDown,
                    });
                break;
            case DoorClickAction.Electrify:
                RaiseLocalEvent(args.EntityUid,
                    new StationAiElectrifiedEvent
                    {
                        User = player,
                        Electrified = !TryComp(args.EntityUid, out ElectrifiedComponent? electrified) || !electrified.Enabled,
                    });
                break;
        }

        return true;
    }
}
