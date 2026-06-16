using System.Numerics;
using Content.Shared.ADT.Implants.SecondHeartImplant;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Implants.SecondHeartImplant;

public sealed class SecondHeartVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public SecondHeartVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index<ShaderPrototype>("SecondHeartVision").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { Valid: true } player
            || !_entities.TryGetComponent<SecondHeartVisionComponent>(player, out _))
            return false;

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        if (_player.LocalEntity is not { Valid: true } player
            || !_entities.TryGetComponent<SecondHeartVisionComponent>(player, out var comp))
            return;

        var elapsed = (float)(_timing.CurTime - comp.StartTime).TotalSeconds;
        elapsed = Math.Max(0f, elapsed);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("elapsed", elapsed);

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }
}
