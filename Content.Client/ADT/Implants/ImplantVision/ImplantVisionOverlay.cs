using System.Numerics;
using Content.Shared.ADT.Implants.ImplantVision;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Implants.ImplantVision;

public sealed class ImplantVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ImplantVisionPrototype _vision;
    private readonly ShaderInstance _shader;
    private readonly TransformSystem _transform;

    public ImplantVisionOverlay(ImplantVisionPrototype vision)
    {
        IoCManager.InjectDependencies(this);

        _vision = vision;
        _transform = _entities.System<TransformSystem>();
        _shader = _prototype.Index<ShaderPrototype>(vision.Shader).InstanceUnique();

        if (vision.Tint is { } tint)
        {
            _shader.SetParameter("tint_color", new Vector3(tint.R, tint.G, tint.B));
            _shader.SetParameter("tint_amount", vision.TintAmount);
        }

        if (vision.AboveEffects)
            ZIndex = (int) Content.Shared.DrawDepth.DrawDepth.Effects + 20;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return TryGetState(out _, out _) && base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null || !TryGetState(out var player, out var state))
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        if (_vision.Timing != ImplantVisionTiming.None)
        {
            var elapsed = (float) (_timing.CurTime - state.Start).TotalSeconds;
            if (state.End is { } end)
            {
                var total = (float) (end - state.Start).TotalSeconds;
                elapsed = Math.Clamp(elapsed, 0f, total);

                if (_vision.Timing == ImplantVisionTiming.ElapsedAndTotal)
                    _shader.SetParameter("total", total);
            }

            _shader.SetParameter("elapsed", Math.Max(0f, elapsed));
        }

        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);

        if (_vision.DrawUserSprite && args.Viewport.Eye is { } eye
            && _entities.TryGetComponent<SpriteComponent>(player, out var sprite)
            && _entities.TryGetComponent<TransformComponent>(player, out var xform))
        {
            sprite.Render(handle, eye.Rotation, _transform.GetWorldRotation(xform), null, _transform.GetWorldPosition(xform));
        }
    }

    private bool TryGetState(out EntityUid player, out ImplantVisionState state)
    {
        state = default!;
        player = default;

        if (_player.LocalEntity is not { Valid: true } uid
            || !_entities.TryGetComponent<ImplantVisionComponent>(uid, out var comp)
            || !comp.Effects.TryGetValue(_vision.ID, out var found)
            || found.End is { } end && end <= _timing.CurTime)
        {
            return false;
        }

        player = uid;
        state = found;
        return true;
    }
}
