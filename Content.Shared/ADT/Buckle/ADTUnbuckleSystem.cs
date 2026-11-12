using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Buckle;

public sealed class ADTUnbuckleSystem : EntitySystem
{
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Unbuckle,
                InputCmdHandler.FromDelegate(HandleUnbuckle, handle: false, outsidePrediction: false))
            .Register<ADTUnbuckleSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<ADTUnbuckleSystem>();
    }

    private void HandleUnbuckle(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } player)
            return;

        if (!TryComp<BuckleComponent>(player, out var buckle) || !buckle.Buckled)
            return;

        if (TryComp<HandsComponent>(player, out var hands)
            && _hands.TryGetHeldItem((player, hands), hands.ActiveHandId, out _, hideVirtualItems: true))
        {
            return;
        }

        if (!_blocker.CanInteract(player, null))
            return;

        _buckle.TryUnbuckle((player, buckle), player, popup: true);
    }
}
