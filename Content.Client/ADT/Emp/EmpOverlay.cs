using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Emp;

public sealed partial class EmpOverlay : Overlay
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IClyde _clyde = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private ShaderInstance _datamoshShader;
    private const string DatamoshShaderProto = "Datamosh";
    private const float BlockSize = 16f;

    private bool _isActive = false;
    private IRenderTexture? _prevFrame;

    private TimeSpan _startTime;
    private TimeSpan _endTime;
    private float _motionStrength;
    private float _smear;
    private float _glitch;

    public EmpOverlay()
    {
        IoCManager.InjectDependencies(this);

        _datamoshShader = _proto.Index<ShaderPrototype>(DatamoshShaderProto).InstanceUnique();
    }

    public void DoEffect(float duration, float motionStrength, float smear, float glitch)
    {
        _isActive = true;
        _startTime = _timing.CurTime;
        _endTime = _timing.CurTime + TimeSpan.FromSeconds(duration);

        _motionStrength = motionStrength;
        _smear = smear;
        _glitch = glitch;
    }

    public void Stop()
    {
        _isActive = false;
        _prevFrame?.Dispose();
        _prevFrame = null;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _isActive;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var curDiff = _timing.CurTime - _startTime;
        var endDiff = _endTime - _startTime;
        var progress = (float)curDiff.TotalSeconds / (float)endDiff.TotalSeconds;

        if (progress > 1)
        {
            Stop();
        }

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
        _datamoshShader.SetParameter("uMotionStrength", _motionStrength);
        _datamoshShader.SetParameter("uSmear", _smear);
        _datamoshShader.SetParameter("uGlitch", _glitch);
        _datamoshShader.SetParameter("uResolution", ScreenTexture.Size);

        worldHandle.UseShader(_datamoshShader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }
}
