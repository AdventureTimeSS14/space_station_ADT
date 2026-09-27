using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Input;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Resist;

public sealed class ADTResistSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly ProtoId<AlertPrototype> FireAlert = "Fire";

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Resist,
                InputCmdHandler.FromDelegate(HandleResist, handle: false, outsidePrediction: false))
            .Register<ADTResistSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<ADTResistSystem>();
    }

    private void HandleResist(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } uid)
            return;

        if (!_blocker.CanInteract(uid, null))
            return;

        if (TryComp<PullableComponent>(uid, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(uid, pullable, uid);
            return;
        }

        if (!TryComp<FlammableComponent>(uid, out var flammable) || !flammable.OnFire || flammable.Resisting)
            return;

        if (!_proto.TryIndex(FireAlert, out var alert))
            return;

        _alerts.ActivateAlert(uid, alert);
    }
}
