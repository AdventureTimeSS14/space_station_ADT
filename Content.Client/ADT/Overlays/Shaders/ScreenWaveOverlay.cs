using Content.Shared.ADT.Drugs;
using Content.Shared.CCVar;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Overlays;

public sealed partial class ScreenWaveOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "ScreenRotation";

    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IEntitySystemManager _sysMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    private readonly StatusEffectsSystem _statusEffects = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _shader;

    public float Intoxication = 0.0f;
    public float TimeTicker = 0.0f;

    private const float VisualThreshold = 10.0f;
    private const float PowerDivisor = 200.0f;
    private const float MaxRotationAngle = 0.1f;
    private const float RotationFrequency = 1.5f;
    private const float FadeOutSpeed = 40f;
    private float _warpScale = 0.0f;

    private float EffectScale => Math.Clamp((Intoxication - VisualThreshold) / PowerDivisor, 0.0f, 1.0f);

    public ScreenWaveOverlay()
    {
        IoCManager.InjectDependencies(this);

        _statusEffects = _sysMan.GetEntitySystem<StatusEffectsSystem>();

        _shader = _prototypeManager.Index(Shader).InstanceUnique();
        _config.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);
    }

    private void OnReducedMotionChanged(bool reducedMotion)
    {
        _warpScale = reducedMotion ? 0.0f : 1.0f;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        TimeTicker += args.DeltaSeconds;
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null)
            return;

        if (_statusEffects.TryGetEffectsEndTimeWithComp<ScreenWaveComponent>(playerEntity, out var endTime) && endTime <= _timing.CurTime)
        {
            Intoxication = MathF.Max(0.0f, Intoxication - FadeOutSpeed * args.DeltaSeconds);
            return;
        }

        endTime ??= TimeSpan.MaxValue;
        var timeLeft = (float)(endTime - _timing.CurTime).Value.TotalSeconds;

        if (timeLeft - TimeTicker > timeLeft / 16f)
        {
            Intoxication += (timeLeft - Intoxication) * args.DeltaSeconds / 16f;
        }
        else
        {
            Intoxication -= Intoxication / (timeLeft - TimeTicker) * args.DeltaSeconds;
        }
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return EffectScale > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var angle = MathF.Sin(TimeTicker * RotationFrequency) * MaxRotationAngle * EffectScale * _warpScale;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("angle", angle);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
