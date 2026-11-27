using System.Numerics;
using Content.Shared.ADT.EMP;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Emp;

public sealed class EmpOverlay : Overlay
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly ProtoId<ShaderPrototype> DatamoshShader = "Datamosh";
    private const float BlockSize = 16f;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _datamoshShader;
    private IRenderTexture? _prevFrame;
    private EmpGlitchComponent? _glitch;
    private TimeSpan _glitchStart;

    public EmpOverlay()
    {
        IoCManager.InjectDependencies(this);

        _datamoshShader = _proto.Index(DatamoshShader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entMan.TryGetComponent(_player.LocalEntity, out _glitch) || _timing.CurTime >= _glitch.GlitchEnd)
        {
            ResetFrame();
            return false;
        }

        if (_glitch.GlitchStart != _glitchStart)
        {
            ResetFrame();
            _glitchStart = _glitch.GlitchStart;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || _glitch == null)
            return;

        var progress = (float)((_timing.CurTime - _glitch.GlitchStart) / (_glitch.GlitchEnd - _glitch.GlitchStart));

        var worldHandle = args.WorldHandle;
        var viewport = args.Viewport;

        if (_prevFrame == null)
        {
            var frameSize = viewport.RenderTarget.Size;
            _prevFrame = _clyde.CreateRenderTarget(
                frameSize,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "prev-frame-emp");
            worldHandle.RenderInRenderTarget(_prevFrame, () =>
            {
                worldHandle.DrawTextureRect(
                    viewport.RenderTarget.Texture,
                    new Box2(Vector2.Zero, new Vector2(frameSize.X, frameSize.Y)));
            }, Color.Transparent);

            _datamoshShader.SetParameter("PREV_TEXTURE", _prevFrame.Texture);
        }

        _datamoshShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _datamoshShader.SetParameter("uBlockSize", BlockSize);
        _datamoshShader.SetParameter("uProgress", progress);
        _datamoshShader.SetParameter("uMotionStrength", _glitch.MotionStrength);
        _datamoshShader.SetParameter("uSmear", _glitch.Smear);
        _datamoshShader.SetParameter("uGlitch", _glitch.Glitch);
        _datamoshShader.SetParameter("uResolution", ScreenTexture.Size);

        worldHandle.UseShader(_datamoshShader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }

    private void ResetFrame()
    {
        _prevFrame?.Dispose();
        _prevFrame = null;
    }

    protected override void DisposeBehavior()
    {
        ResetFrame();
        base.DisposeBehavior();
    }
}
