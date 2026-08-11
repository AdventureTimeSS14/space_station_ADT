using Content.Client.Examine;
using Content.Shared.ADT.Silicons;
using Content.Shared.Doors.Components;
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
            .BindBefore(ContentKeyFunctions.ExamineEntity, new PointerInputCmdHandler(OnShiftClick, outsidePrediction: true), typeof(ExamineSystem))
            .BindBefore(ContentKeyFunctions.TryPullObject, new PointerInputCmdHandler(OnCtrlClick, outsidePrediction: true), typeof(SharedInteractionSystem))
            .BindBefore(ContentKeyFunctions.AltActivateItemInWorld, new PointerInputCmdHandler(OnAltClick, outsidePrediction: true), typeof(SharedInteractionSystem))
            .Register<DoorClickSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<DoorClickSystem>();
        base.Shutdown();
    }

    private bool OnShiftClick(in PointerInputCmdArgs args) => TryDoorClick(args, DoorClickAction.EmergencyAccess);

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
        return true;
    }
}
