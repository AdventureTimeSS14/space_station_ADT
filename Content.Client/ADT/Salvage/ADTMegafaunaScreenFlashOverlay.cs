using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Salvage;

public sealed class ADTMegafaunaScreenFlashOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ColorTintShader = "ColorTint";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public Vector3 TintColor;

    public TimeSpan StartTime;

    public TimeSpan Duration;

    public ADTMegafaunaScreenFlashOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototype.Index(ColorTintShader).InstanceUnique();
    }

    public bool Finished => _timing.RealTime >= StartTime + Duration;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return !Finished;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || Duration <= TimeSpan.Zero)
            return;

        var elapsed = (float) ((_timing.RealTime - StartTime).TotalSeconds / Duration.TotalSeconds);
        var amount = Math.Clamp(1f - elapsed, 0f, 1f);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("tint_color", TintColor);
        _shader.SetParameter("tint_amount", amount);

        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
