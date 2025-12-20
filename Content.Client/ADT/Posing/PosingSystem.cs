using Content.Shared.ADT.Posing;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;

namespace Content.Client.ADT.Posing;

public sealed partial class PosingSystem : SharedPosingSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PosingComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);

        var posing = _input.Contexts.New("posing", "common");
        posing.AddFunction(ContentKeyFunctions.TogglePosing);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetUp);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetDown);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetLeft);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetRight);
        posing.AddFunction(ContentKeyFunctions.PosingRotatePositive);
        posing.AddFunction(ContentKeyFunctions.PosingRotateNegative);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.Contexts.Remove("posing");
    }

    private void OnAfterHandleState(EntityUid uid, PosingComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_playerManager.LocalEntity == uid)
            return;

        if (!component.Posing)
        {
            _sprite.SetOffset(uid, component.DefaultOffset);
            _sprite.SetRotation(uid, Angle.FromDegrees(component.DefaultAngle));
            return;
        }
    }

    protected override void ClientTogglePosing(EntityUid uid, PosingComponent posing)
    {
        base.ClientTogglePosing(uid, posing);

        _input.Contexts.SetActiveContext(posing.Posing ? "posing" : posing.DefaultInputContext);
        _sprite.SetOffset(uid, posing.DefaultOffset);
        _sprite.SetRotation(uid, Angle.FromDegrees(posing.DefaultAngle));
    }

    // возможно не самый лучший способ, но вы б знали, как я не хочу возиться со всеми остальными анимациями
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PosingComponent>();
        while (query.MoveNext(out var uid, out var posing))
        {
            if (posing.Posing)
            {
                _sprite.SetOffset(uid, posing.DefaultOffset + posing.CurrentOffset);
                _sprite.SetRotation(uid, posing.CurrentAngle);
            }
        }
    }
}
