using Content.Shared.ADT.Cyberpsychosis;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Cyberpsychosis;

// Handles only the periodic retinal-burn inversion flash for Severe state.
// The main per-entity Blackwall corruption is in BlackwallVisionOverlay.
public sealed class BlackwallScreenOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private double _flashStart = -10.0;
    private double _nextFlashTime = 4.0;
    private readonly Random _rng = new();

    private const double FlashDuration = 0.5;

    public BlackwallScreenOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        return args.Viewport.Eye == eyeComp.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out ActiveCyberpsychosisComponent? active) ||
            active.State != CyberpsychosisState.Severe)
            return;

        var now = _timing.CurTime.TotalSeconds;

        if (now >= _nextFlashTime)
        {
            _flashStart = now;
            _nextFlashTime = now + FlashDuration + 3.5 + _rng.NextDouble() * 5.0;
        }

        var flashProgress = (now - _flashStart) / FlashDuration;
        if (flashProgress is < 0.0 or > 1.0)
            return;

        var handle = args.WorldHandle;

        // First half: white blast (инверсия — выжигание сетчатки)
        // Second half: red fade-out
        float alpha;
        Color flashColor;
        if (flashProgress < 0.4)
        {
            alpha = (float)(flashProgress / 0.4 * 0.95);
            flashColor = new Color(1f, 1f, 1f, alpha);
        }
        else
        {
            alpha = (float)((1.0 - flashProgress) / 0.6 * 0.7f);
            flashColor = new Color(1f, 0.05f, 0.05f, alpha);
        }

        handle.DrawRect(args.WorldBounds, flashColor);
    }
}
