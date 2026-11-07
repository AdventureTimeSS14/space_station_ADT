using System.Numerics;
using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.ADT.Cooking;

public sealed class ADTStoveInputSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .BindBefore(ContentKeyFunctions.AltActivateItemInWorld, new PointerInputCmdHandler(OnAltClick, outsidePrediction: true), typeof(SharedInteractionSystem))
            .Register<ADTStoveInputSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<ADTStoveInputSystem>();
        base.Shutdown();
    }

    private bool OnAltClick(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State != BoundKeyState.Down)
            return false;

        var stove = args.EntityUid;
        if (!stove.IsValid() || !TryComp<ADTStoveComponent>(stove, out var comp))
            return false;

        var world = _transform.ToMapCoordinates(args.Coordinates).Position;
        var local = Vector2.Transform(world, _transform.GetInvWorldMatrix(stove));

        int? best = null;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < comp.Slots.Count; i++)
        {
            var distance = (comp.Slots[i].Offset - local).LengthSquared();
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = i;
        }

        if (best == null || comp.Slots[best.Value].Type != ADTStoveSlotType.Burner)
            return false;

        RaiseNetworkEvent(new ADTStoveAltClickEvent(GetNetEntity(stove), GetNetCoordinates(args.Coordinates)));
        return true;
    }
}
