using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.ADT.RemoteEye.Components;
using Content.Shared.ADT.RemoteEye.Systems;
using Content.Shared.Movement.Components;

namespace Content.Server.ADT.RemoteEye;

public sealed class RemoteEyeSystem : SharedRemoteEyeSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const string ReturnActionPrototype = "ActionReturnFromRemoteEye";

    public override void TransferToEye(Entity<RemoteEyeConsoleComponent> console, EntityUid user)
    {
        if (console.Comp.RemoteEye == null)
            return;

        if (!_mind.TryGetMind(user, out var mindId, out var mind))
            return;

        console.Comp.OriginalEntity = user;
        Dirty(console);

        _mind.TransferTo(mindId, console.Comp.RemoteEye, mind: mind);

        if (TryComp(console.Comp.RemoteEye, out EyeComponent? eyeComp))
        {
            _eye.SetDrawFov(console.Comp.RemoteEye.Value, false, eyeComp);
        }

        _mover.SetRelay(user, console.Comp.RemoteEye.Value);

        var eyeName = Loc.GetString("remote-eye-name", ("name", Name(user)));
        _metadata.SetEntityName(console.Comp.RemoteEye.Value, eyeName);

        _actions.AddAction(console.Comp.RemoteEye.Value, ref console.Comp.ReturnActionEntity, ReturnActionPrototype);

        _popup.PopupEntity(Loc.GetString("remote-eye-transfer-success"), console.Comp.RemoteEye.Value, console.Comp.RemoteEye.Value);
    }

    public override void ReturnFromEye(Entity<RemoteEyeConsoleComponent> console, EntityUid user)
    {
        if (console.Comp.OriginalEntity == null || console.Comp.RemoteEye == null)
            return;

        if (!_mind.TryGetMind(console.Comp.RemoteEye.Value, out var mindId, out var mind))
            return;

        var original = console.Comp.OriginalEntity.Value;

        _mind.TransferTo(mindId, original, mind: mind);

        if (TryComp(console.Comp.RemoteEye, out EyeComponent? eyeComp))
        {
            _eye.SetDrawFov(console.Comp.RemoteEye.Value, true, eyeComp);
        }

        RemCompDeferred<RelayInputMoverComponent>(console.Comp.RemoteEye.Value);
        _actions.RemoveAction(console.Comp.RemoteEye.Value, console.Comp.ReturnActionEntity);

        console.Comp.OriginalEntity = null;
        Dirty(console);

        _popup.PopupEntity(Loc.GetString("remote-eye-return-success"), original, original);
    }
}
