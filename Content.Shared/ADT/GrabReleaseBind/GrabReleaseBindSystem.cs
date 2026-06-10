using Content.Shared.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.ADT.GrabReleaseBind;

public sealed class GrabReleaseBindSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ResistGrab,
                InputCmdHandler.FromDelegate(HandleResistGrab, handle: false, outsidePrediction: false))
            .Register<GrabReleaseBindSystem>();
    }

    private void HandleResistGrab(ICommonSession? session)
    {
        if (session?.AttachedEntity == null || !TryComp<PullableComponent>(session.AttachedEntity, out var pullable))
            return;

        var uid = session.AttachedEntity.Value;
        if (!_blocker.CanInteract(uid, null))
            return;

        _pullingSystem.TryStopPull(uid, pullable, uid);
    }
}
