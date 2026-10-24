using Content.Shared.ADT.Cyberpsychosis;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Cyberpsychosis;

public sealed class BlackwallFlashOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> InversionShader = "BlackwallInversion";

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _inversionShader;
    private readonly Random _rng = new();

    private double _flashStart = -99.0;
    private double _nextFlash = 5.0;

    private const double FlashDuration = 0.2;
    private const double InversionDuration = 0.5;
    private const double ReturnDuration = 0.5;
    private const double TotalDuration = FlashDuration + InversionDuration + ReturnDuration;

    public BlackwallFlashOverlay()
    {
        IoCManager.InjectDependencies(this);
        _inversionShader = _protoManager.Index(InversionShader).InstanceUnique();
        ZIndex = 500;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        return args.Viewport.Eye == eyeComp.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out ActiveCyberpsychosisComponent? active))
            return;

        if (active.State != CyberpsychosisState.Moderate && active.State != CyberpsychosisState.Severe)
            return;

        var now = _timing.CurTime.TotalSeconds;

        if (now >= _nextFlash)
        {
            _flashStart = now;
            _nextFlash = now + TotalDuration + 4.0 + _rng.NextDouble() * 7.0;
        }

        var elapsed = now - _flashStart;
        if (elapsed < 0 || elapsed > TotalDuration)
            return;

        var handle = args.WorldHandle;

        if (elapsed <= FlashDuration)
        {
            var t = elapsed / FlashDuration;
            var flashAlpha = (float)Math.Sin(t * Math.PI);
            handle.DrawRect(args.WorldBounds, new Color(1f, 1f, 1f, flashAlpha));
            return;
        }

        if (ScreenTexture == null)
            return;

        var invElapsed = elapsed - FlashDuration;

        float invAlpha;
        if (invElapsed <= InversionDuration)
        {
            invAlpha = 1f - (float)(invElapsed / InversionDuration);
        }
        else
        {
            var retElapsed = invElapsed - InversionDuration;
            invAlpha = (float)(retElapsed / ReturnDuration) * 0.15f;
            invAlpha = 0.15f - invAlpha;
            invAlpha = Math.Max(0f, invAlpha);
        }

        _inversionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _inversionShader.SetParameter("alpha", invAlpha);
        handle.UseShader(_inversionShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
