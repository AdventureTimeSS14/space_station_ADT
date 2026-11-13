using System.Numerics;
using Content.Shared.ADT.Crushers.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Crushers;

public sealed class CrusherRageOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ColorTintShader = "ColorTint";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    private Vector3 _color;
    private float _minAmount;
    private float _maxAmount;
    private float _frequency;
    private float _flashAmount;
    private TimeSpan _flashDuration;
    private TimeSpan _startTime;

    public CrusherRageOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototype.Index(ColorTintShader).InstanceUnique();
    }

    public void Configure(ADTCrusherRageOverlayStatusEffectComponent comp)
    {
        _color = new Vector3(comp.Color.R, comp.Color.G, comp.Color.B);
        _minAmount = comp.MinAmount;
        _maxAmount = comp.MaxAmount;
        _frequency = comp.PulseFrequency;
        _flashAmount = comp.FlashAmount;
        _flashDuration = comp.FlashDuration;
        _startTime = _timing.RealTime;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var elapsed = (float) (_timing.RealTime - _startTime).TotalSeconds;
        var pulse = (MathF.Sin(elapsed * _frequency * MathF.Tau) + 1f) / 2f;
        var amount = _minAmount + (_maxAmount - _minAmount) * pulse;

        if (_flashDuration > TimeSpan.Zero && elapsed < _flashDuration.TotalSeconds)
        {
            var flash = _flashAmount * (1f - elapsed / (float) _flashDuration.TotalSeconds);
            amount = MathF.Max(amount, flash);
        }

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("tint_color", _color);
        _shader.SetParameter("tint_amount", amount);

        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
