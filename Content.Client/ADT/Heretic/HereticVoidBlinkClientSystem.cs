using Content.Shared.ADT.Heretic;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client.ADT.Heretic;

public sealed class HereticVoidBlinkClientSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MouseMiddle, new PointerInputCmdHandler(OnMiddleClick, outsidePrediction: true))
            .Register<HereticVoidBlinkClientSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<HereticVoidBlinkClientSystem>();
        base.Shutdown();
    }

    private bool OnMiddleClick(in PointerInputCmdArgs args)
    {
        if (_player.LocalEntity is not { } player)
            return false;

        if (!_heretic.TryGetHereticComponent(player, out var heretic, out _))
            return false;

        if (heretic.CurrentPath != "Void")
            return false;

        if (TryComp<HandsComponent>(player, out var hands))
        {
            if (_hands.TryGetHeldItem((player, hands), hands.ActiveHandId, out var held))
            {
                if (!HasComp<HereticBladeComponent>(held.Value))
                    return false;
            }
        }

        RaiseNetworkEvent(new HereticVoidBlinkRequestEvent(GetNetEntity(args.EntityUid), GetNetCoordinates(args.Coordinates)));
        return true;
    }
}
