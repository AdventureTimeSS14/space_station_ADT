using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Explosion;

public sealed class ADTShockwaveOverlay : Overlay
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "ADTBlastShockwave";

    public const int MaxCount = 5;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private readonly List<Shockwave> _waves = new();

    private readonly Vector2[] _positions = new Vector2[MaxCount];
    private readonly float[] _radii = new float[MaxCount];
    private readonly float[] _widths = new float[MaxCount];
    private readonly float[] _strengths = new float[MaxCount];
    private int _count;

    public ADTShockwaveOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index(Shader).Instance().Duplicate();
        ZIndex = 102;
    }

    public void Add(MapCoordinates coords, float maxRadius, TimeSpan duration)
    {
        _waves.Add(new Shockwave(coords, maxRadius, _timing.CurTime, duration));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return false;

        var now = _timing.CurTime;
        _waves.RemoveAll(w => now >= w.Start + w.Duration);

        _count = 0;
        var pixelScale = EyeManager.PixelsPerMeter * args.Viewport.RenderScale.X * args.Viewport.Eye.Scale.X;

        foreach (var wave in _waves)
        {
            if (wave.Coords.MapId != args.MapId)
                continue;

            var progress = (float) ((now - wave.Start).TotalSeconds / wave.Duration.TotalSeconds);
            var local = args.Viewport.WorldToLocal(wave.Coords.Position);
            local.Y = args.Viewport.Size.Y - local.Y;

            _positions[_count] = local;
            _radii[_count] = progress * wave.MaxRadius * pixelScale;
            _widths[_count] = 0.6f * pixelScale;
            _strengths[_count] = (1f - progress) * 0.25f * pixelScale;
            _count++;

            if (_count == MaxCount)
                break;
        }

        return _count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("count", _count);
        _shader.SetParameter("position", _positions);
        _shader.SetParameter("radius", _radii);
        _shader.SetParameter("width", _widths);
        _shader.SetParameter("strength", _strengths);

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldAABB, Color.White);
        handle.UseShader(null);
    }

    private readonly record struct Shockwave(MapCoordinates Coords, float MaxRadius, TimeSpan Start, TimeSpan Duration);
}
