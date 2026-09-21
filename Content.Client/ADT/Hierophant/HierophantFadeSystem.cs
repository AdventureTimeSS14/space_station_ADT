using Content.Shared.ADT.Hierophant;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Hierophant;

public sealed class HierophantFadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<HierophantFadeComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out var fade, out var sprite))
        {
            var elapsed = now - fade.StartTime;
            var progress = fade.Duration.TotalSeconds <= 0
                ? 1f
                : (float) (elapsed.TotalSeconds / fade.Duration.TotalSeconds);

            progress = Math.Clamp(progress, 0f, 1f);

            var alpha = float.Lerp(fade.StartAlpha, fade.EndAlpha, progress);
            var color = sprite.Color;
            _sprite.SetColor((uid, sprite), color.WithAlpha(alpha));

            if (progress >= 1f)
                RemCompDeferred<HierophantFadeComponent>(uid);
        }
    }
}
