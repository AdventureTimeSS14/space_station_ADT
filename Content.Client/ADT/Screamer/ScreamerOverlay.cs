using System.Linq;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Matrix3x2 = System.Numerics.Matrix3x2;

namespace Content.Client.ADT.Screamer;

public sealed class ScreamerOverlay : Overlay
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly SharedTransformSystem _xformSystem;
    private readonly SpriteSystem _sprite;

    public override bool RequestScreenTexture => false;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private Dictionary<EntityUid, ScreamerData> _activeScreamers = new();
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;

    public ScreamerOverlay()
    {
        IoCManager.InjectDependencies(this);

        _xformSystem = _entity.System<SharedTransformSystem>();
        _sprite = _entity.System<SpriteSystem>();

        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _xformQuery = _entity.GetEntityQuery<TransformComponent>();
    }

    public void AddScreamer(EntityUid entity, Vector2 offset, float duration, float alpha, bool fadeIn, bool fadeOut)
    {
        var data = new ScreamerData()
        {
            EndTime = duration > 0 ? _timing.CurTime + TimeSpan.FromSeconds(duration) : null,
            Duration = duration,
            FadeIn = fadeIn,
            FadeOut = fadeOut,
            Offset = offset,
            Alpha = alpha
        };

        _activeScreamers.Add(entity, data);
    }

    public void Clear()
    {
        for (var i = _activeScreamers.Count - 1; i >= 0; i--)
        {
            var ent = _activeScreamers.ElementAt(i).Key;

            _activeScreamers.Remove(ent);
            _entity.QueueDeleteEntity(ent);
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye is not { } eye)
            return;

        if (_player.LocalEntity is not { Valid: true } player || !_xformQuery.TryComp(player, out var xform))
            return;

        if (_activeScreamers.Count <= 0)
            return;

        var handle = args.WorldHandle;
        var eyeRot = eye.Rotation;

        for (var i = _activeScreamers.Count - 1; i >= 0; i--)
        {
            var item = _activeScreamers.ElementAt(i);
            if (item.Value.EndTime != null && item.Value.EndTime <= _timing.CurTime)
            {
                var ent = item.Key;
                _activeScreamers.Remove(ent);
                _entity.QueueDeleteEntity(ent);
                continue;
            }

            if (!_spriteQuery.TryComp(item.Key, out var sprite))
                continue;

            var alpha = GetAlpha(item.Value) * item.Value.Alpha;

            RenderEntity((item.Key, sprite), (player, xform), handle, eyeRot, alpha, item.Value.Offset, Color.White);
        }
    }

    private float GetAlpha(ScreamerData data)
    {
        if (data.EndTime == null)
            return 1f;

        var timeLeft = (data.EndTime.Value - _timing.CurTime).TotalSeconds;
        var elapsed = data.Duration - timeLeft;
        var segment = data.Duration / 3f;

        float factor = 1f;
        if (data.FadeIn && elapsed < segment)
        {
            factor = (float)(elapsed / segment);
        }
        else if (data.FadeOut)
        {
            if (data.FadeIn && elapsed > segment * 2)
                factor = 1f - (float)((elapsed - segment * 2) / segment);
            else if (!data.FadeIn)
                factor = 1f - (float)(elapsed / data.Duration);
        }
        // If not fading in/out, factor remains 1f

        return factor;
    }

    private void RenderEntity(
        Entity<SpriteComponent> ent,
        Entity<TransformComponent> player,
        DrawingHandleWorld handle,
        Angle eyeRot,
        float alpha,
        Vector2 offset,
        Color color)
    {
        var position = _xformSystem.GetWorldPosition(player.Comp);
        //var rotation = _xformSystem.GetWorldRotation(player.Comp);

        handle.SetTransform(position + offset, eyeRot);

        var originalColor = ent.Comp.Color;

        _sprite.SetColor(ent.Owner, color.WithAlpha(alpha));
        _sprite.RenderSprite(ent, handle, eyeRot, eyeRot, position + offset);

        _sprite.SetColor(ent.Owner, originalColor);
        handle.SetTransform(Vector2.Zero, Angle.Zero);
    }

    private struct ScreamerData
    {
        public TimeSpan? EndTime;
        public float Duration;
        public bool FadeIn;
        public bool FadeOut;
        public Vector2 Offset;
        public float Alpha;
    }
}
