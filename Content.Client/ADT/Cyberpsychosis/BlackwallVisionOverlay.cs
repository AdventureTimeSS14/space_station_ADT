using System.Numerics;
using Content.Shared.ADT.Cyberpsychosis;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Matrix3x2 = System.Numerics.Matrix3x2;

namespace Content.Client.ADT.Cyberpsychosis;

public sealed class BlackwallVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SpriteSystem _spriteSystem;
    private readonly SharedTransformSystem _xformSystem;
    private readonly ContainerSystem _container;
    private readonly EntityQuery<MobStateComponent> _mobQuery;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private const float MaxRenderDistance = 10f;
    private readonly List<(EntityUid Uid, SpriteComponent Sprite, TransformComponent Xform)> _entries = new(64);

    public BlackwallVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entity.System<SpriteSystem>();
        _container = _entity.System<ContainerSystem>();
        _xformSystem = _entity.System<SharedTransformSystem>();
        _mobQuery = _entity.GetEntityQuery<MobStateComponent>();
        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _xformQuery = _entity.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var localEntity = _player.LocalEntity;
        if (localEntity == null)
            return;

        if (!_entity.TryGetComponent(localEntity, out ActiveCyberpsychosisComponent? active) ||
            active.State != CyberpsychosisState.Severe)
            return;

        var eye = args.Viewport.Eye;
        if (eye == null)
            return;

        var mapId = eye.Position.MapId;
        var eyeRot = eye.Rotation;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;

        var playerPos = Vector2.Zero;
        var hasPlayerPos = false;
        if (_xformQuery.TryGetComponent(localEntity, out var playerXform))
        {
            playerPos = _xformSystem.GetWorldPosition(playerXform);
            hasPlayerPos = true;
        }

        foreach (var ent in _entity.GetEntities())
        {
            if (ent == localEntity) continue;
            if (!_mobQuery.TryGetComponent(ent, out var mob) || mob.CurrentState == MobState.Dead) continue;
            if (!_spriteQuery.TryGetComponent(ent, out var sprite)) continue;
            if (!_xformQuery.TryGetComponent(ent, out var xform)) continue;
            if (_container.TryGetOuterContainer(ent, xform, out _)) continue;
            if (xform.MapID != mapId) continue;

            var worldPos = _xformSystem.GetWorldPosition(xform);
            if (!worldBounds.Contains(worldPos)) continue;
            if (hasPlayerPos && (worldPos - playerPos).Length() > MaxRenderDistance) continue;

            _entries.Add((ent, sprite, xform));
        }

        foreach (var (uid, sprite, xform) in _entries)
            RenderBlackwall(uid, sprite, xform, worldHandle, eyeRot);

        _entries.Clear();
    }

    private void RenderBlackwall(
        EntityUid uid,
        SpriteComponent sprite,
        TransformComponent xform,
        DrawingHandleWorld handle,
        Angle eyeRot)
    {
        var position = _xformSystem.GetWorldPosition(xform);
        var rotation = _xformSystem.GetWorldRotation(xform);
        var t = (float)_timing.CurTime.TotalSeconds;
        var seed = (uid.Id % 997u) / 997f;
        var originalColor = sprite.Color;

        handle.SetTransform(Matrix3x2.Identity);

        var outlineSize = 0.048f;
        var burgundy = new Color(0.38f, 0f, 0.07f, 1f);

        sprite.Color = burgundy;
        _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, position + new Vector2(-outlineSize, 0f), null);
        _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, position + new Vector2(outlineSize, 0f), null);
        _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, position + new Vector2(0f, -outlineSize), null);
        _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, position + new Vector2(0f, outlineSize), null);

        sprite.Color = Color.Black;
        _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, position, null);

        DrawTearingChunks(uid, sprite, handle, eyeRot, position, rotation, seed, t);

        sprite.Color = originalColor;

        handle.SetTransform(Matrix3Helpers.CreateTransform(position, rotation));
        DrawContinuousStreams(handle, seed, t);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTearingChunks(
        EntityUid uid,
        SpriteComponent sprite,
        DrawingHandleWorld handle,
        Angle eyeRot,
        Vector2 position,
        Angle rotation,
        float seed,
        float t)
    {
        for (var c = 0; c < 2; c++)
        {
            var cSeed = c * 0.5f;
            var timeBin = MathF.Floor(t * 2.5f + cSeed * 7f);
            var visSeed = Fract(MathF.Sin(seed * 99.1f + cSeed * 33.3f + timeBin * 0.7f) * 43758.5453f);
            if (visSeed < 0.4f)
                continue;

            var ox = (Fract(MathF.Sin(seed * 78.9f + cSeed * 55.5f + timeBin) * 43758.5453f) * 2f - 1f) * 0.65f;
            var oy = (Fract(MathF.Sin(seed * 31.4f + cSeed * 22.2f + timeBin) * 23456.789f) * 2f - 1f) * 0.3f;
            var chunkPos = position + new Vector2(ox, oy);

            // Широкий glow (дальние смещения)
            var glow = new Color(0.5f, 0f, 0.08f, 0.18f);
            const float glowOff = 0.072f;
            sprite.Color = glow;
            _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, chunkPos + new Vector2(-glowOff, -glowOff), null);
            _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, chunkPos + new Vector2(glowOff, -glowOff), null);
            _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, chunkPos + new Vector2(-glowOff, glowOff), null);
            _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, chunkPos + new Vector2(glowOff, glowOff), null);

            // Бордовый контур (близкие смещения)
            var outline = new Color(0.55f, 0f, 0.09f, 0.9f);
            const float outlineOff = 0.042f;
            sprite.Color = outline;
            _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, chunkPos + new Vector2(-outlineOff, 0f), null);
            _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, chunkPos + new Vector2(outlineOff, 0f), null);
            _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, chunkPos + new Vector2(0f, -outlineOff), null);
            _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, chunkPos + new Vector2(0f, outlineOff), null);

            // Чёрная заливка
            sprite.Color = Color.Black;
            _spriteSystem.RenderSprite((uid, sprite), handle, eyeRot, rotation, chunkPos, null);
        }
    }

    private static void DrawPillarLayer(DrawingHandleWorld handle, float cx, float hw, float yBot, float yTop, Color color)
    {
        handle.DrawRect(new Box2(cx - hw, yBot, cx + hw, yTop), color);
    }

    private static void DrawPillar(DrawingHandleWorld handle, float cx, float fillW, float lineW, float yBot, float yTop, float alpha)
    {
        DrawPillarLayer(handle, cx, fillW + lineW * 5f, yBot, yTop, new Color(0.5f, 0f, 0.08f, 0.08f * alpha));
        DrawPillarLayer(handle, cx, fillW + lineW * 2.5f, yBot, yTop, new Color(0.55f, 0f, 0.09f, 0.2f * alpha));
        DrawPillarLayer(handle, cx, fillW + lineW, yBot, yTop, new Color(0.6f, 0f, 0.1f, 0.9f * alpha));
        DrawPillarLayer(handle, cx, fillW, yBot, yTop, new Color(0f, 0f, 0f, 0.95f * alpha));
    }

    private static void DrawContinuousStreams(DrawingHandleWorld handle, float seed, float t)
    {
        const float halfLen = 3.5f;
        const float fadeLen = 0.8f;
        const int fadeSteps = 6;
        const int pillarCount = 5;
        const float lineW = 0.010f;
        const float fillW = 0.014f;

        for (var i = 0; i < pillarCount; i++)
        {
            var xSeed = Fract(MathF.Sin(seed * 78.9f + i * 127.1f) * 43758.5453f);
            var cx = -0.55f + xSeed * 1.1f;

            var lenSeed = Fract(MathF.Sin(seed * 44.1f + i * 83.7f) * 98765.4321f);
            var pillarHalf = 1.2f + lenSeed * (halfLen - 1.2f - fadeLen);

            var solidBot = -pillarHalf + fadeLen;
            var solidTop = pillarHalf - fadeLen;

            DrawPillar(handle, cx, fillW, lineW, solidBot, solidTop, 1f);

            var step_h = fadeLen / fadeSteps;
            for (var s = 0; s < fadeSteps; s++)
            {
                var frac = 1f - (s + 1f) / fadeSteps;
                var y0 = solidTop + s * step_h;
                var y1 = y0 + step_h;
                DrawPillar(handle, cx, fillW, lineW, y0, y1, frac);
                DrawPillar(handle, cx, fillW, lineW, -y1, -y0, frac);
            }

            // Скользящий пакет движется только внутри своего столбика
            var speed = 0.7f + xSeed * 0.9f;
            var pLen = 0.2f + xSeed * 0.25f;
            var pT = Fract(t * speed + xSeed * 3.7f);
            var pTop = pillarHalf - pT * pillarHalf * 2f;
            var pBot = pTop - pLen;

            DrawPillarLayer(handle, cx, fillW + lineW * 4f, pBot, pTop, new Color(0.7f, 0f, 0.1f, 0.15f));
            DrawPillarLayer(handle, cx, fillW + lineW * 1.5f, pBot, pTop, new Color(0.75f, 0f, 0.1f, 0.4f));
            DrawPillarLayer(handle, cx, fillW - lineW * 0.3f, pBot, pTop, new Color(0.9f, 0.1f, 0.15f, 0.95f));
        }
    }

    private static float Fract(float f) => f - MathF.Floor(f);
}
