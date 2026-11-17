using Content.Shared.ADT.HolyDamage;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Chaplain.Overlays;

public sealed class HolySuppressionOverlay : Overlay
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "ADTHolySuppressionShader";
    private const float MaxIntensity = 0.85f;
    private const float FadeSpeed = 1.5f;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private float _intensity;

    public HolySuppressionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(Shader).InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var target = 0f;
        if (_entity.TryGetComponent<HolySuppressionComponent>(_player.LocalEntity, out var suppression) && suppression.Max > 0)
            target = Math.Clamp((suppression.Amount / suppression.Max).Float(), 0f, MaxIntensity);

        var step = FadeSpeed * args.DeltaSeconds;
        _intensity = Math.Clamp(target, _intensity - step, _intensity + step);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_intensity <= 0f)
            return false;

        return _entity.TryGetComponent(_player.LocalEntity, out EyeComponent? eye) && args.Viewport.Eye == eye.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("Intensity", _intensity);

        var handle = args.ScreenHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.ViewportBounds, Color.White);
        handle.UseShader(null);
    }
}
