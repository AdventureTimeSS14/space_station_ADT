using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Bubblegum;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Client.ADT.Bubblegum;

public sealed class BubblegumDeathVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const string DeathKey = "bubblegum_death";
    private const string SoulKey = "bubblegum_soul";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumDeathVisualsComponent, ComponentInit>(OnDeathInit);
        SubscribeLocalEvent<BubblegumSoulRiseComponent, ComponentInit>(OnSoulInit);
    }

    private void OnDeathInit(Entity<BubblegumDeathVisualsComponent> ent, ref ComponentInit args)
    {
        if (TerminatingOrDeleted(ent.Owner) || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        FreezeSprite((ent.Owner, sprite));

        var player = EnsureComp<AnimationPlayerComponent>(ent);
        if (_anim.HasRunningAnimation(ent.Owner, player, DeathKey))
            return;

        _anim.Play((ent.Owner, player), BuildDeathAnimation(ent, sprite), DeathKey);
    }

    private void OnSoulInit(Entity<BubblegumSoulRiseComponent> ent, ref ComponentInit args)
    {
        if (TerminatingOrDeleted(ent.Owner) || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        FreezeSprite((ent.Owner, sprite));

        var player = EnsureComp<AnimationPlayerComponent>(ent);
        if (_anim.HasRunningAnimation(ent.Owner, player, SoulKey))
            return;

        _anim.Play((ent.Owner, player), BuildSoulAnimation(ent, sprite), SoulKey);
    }

    private void FreezeSprite(Entity<SpriteComponent> ent)
    {
        if (ent.Comp.AllLayers.Any())
            _sprite.LayerSetAutoAnimated(ent.AsNullable(), 0, false);

        ent.Comp.DirectionOverride = Direction.South;
        ent.Comp.EnableDirectionOverride = true;
    }

    private Animation BuildDeathAnimation(Entity<BubblegumDeathVisualsComponent> ent, SpriteComponent sprite)
    {
        var comp = ent.Comp;
        var convulse = (float)comp.ConvulseDuration.TotalSeconds;
        var implode = (float)comp.ImplodeDuration.TotalSeconds;

        var baseScale = sprite.Scale;
        var rage = comp.RageColor;
        var husk = comp.HuskColor;
        var topple = Angle.FromDegrees(comp.ToppleDegrees);

        var throes = MathF.Min(0.8f, convulse * 0.3f);
        var toppleTime = MathF.Min(0.5f, (convulse - throes) * 0.4f);
        var slump = MathF.Max(0.05f, convulse - throes - toppleTime);

        var colorFrames = new List<AnimationTrackProperty.KeyFrame>
        {
            new(Color.White, 0f),
            new(rage, throes * 0.25f),
            new(Color.White, throes * 0.25f),
            new(rage, throes * 0.25f),
            new(husk, throes * 0.25f + toppleTime),
            new(husk, slump),
            new(husk.WithAlpha(0f), implode),
        };

        var scaleFrames = new List<AnimationTrackProperty.KeyFrame>
        {
            new(baseScale, 0f),
            new(baseScale * 1.06f, throes * 0.25f),
            new(baseScale * 0.95f, throes * 0.25f),
            new(baseScale * 1.06f, throes * 0.25f),
            new(baseScale * 0.9f, throes * 0.25f + toppleTime),
            new(baseScale * 0.9f, slump),
            new(new Vector2(0.02f, 0.02f), implode),
        };

        var rotationFrames = new List<AnimationTrackProperty.KeyFrame>
        {
            new(Angle.Zero, 0f),
            new(Angle.Zero, throes),
            new(topple, toppleTime, Easings.OutBounce),
            new(topple, slump),
            new(topple + Angle.FromDegrees(25), implode),
        };

        var offsetFrames = new List<AnimationTrackProperty.KeyFrame> { new(Vector2.Zero, 0f) };
        var shakeFrames = 8;
        for (var i = 0; i < shakeFrames; i++)
        {
            var amp = 0.2f * (1f - (float)i / shakeFrames) + 0.03f;
            var jitter = new Vector2(_random.NextFloat(-amp, amp), _random.NextFloat(-amp, amp));
            offsetFrames.Add(new AnimationTrackProperty.KeyFrame(jitter, throes / shakeFrames));
        }
        offsetFrames.Add(new AnimationTrackProperty.KeyFrame(new Vector2(0f, -0.15f), toppleTime));
        offsetFrames.Add(new AnimationTrackProperty.KeyFrame(new Vector2(0f, -0.15f), slump));
        offsetFrames.Add(new AnimationTrackProperty.KeyFrame(Vector2.Zero, implode));

        return new Animation
        {
            Length = TimeSpan.FromSeconds(convulse + implode),
            AnimationTracks =
            {
                ColorTrack(colorFrames, AnimationInterpolationMode.Linear),
                ScaleTrack(scaleFrames, AnimationInterpolationMode.Cubic),
                RotationTrack(rotationFrames),
                OffsetTrack(offsetFrames, AnimationInterpolationMode.Nearest),
            },
        };
    }

    private Animation BuildSoulAnimation(Entity<BubblegumSoulRiseComponent> ent, SpriteComponent sprite)
    {
        var length = (float)ent.Comp.Duration.TotalSeconds;
        var rise = ent.Comp.RiseHeight;
        var baseColor = sprite.Color;
        var baseScale = sprite.Scale;

        return new Animation
        {
            Length = ent.Comp.Duration,
            AnimationTracks =
            {
                OffsetTrack(new List<AnimationTrackProperty.KeyFrame>
                {
                    new(Vector2.Zero, 0f),
                    new(new Vector2(0f, rise), length),
                }, AnimationInterpolationMode.Linear),
                ColorTrack(new List<AnimationTrackProperty.KeyFrame>
                {
                    new(baseColor, 0f),
                    new(baseColor, length * 0.35f),
                    new(baseColor.WithAlpha(0f), length * 0.65f),
                }, AnimationInterpolationMode.Linear),
                ScaleTrack(new List<AnimationTrackProperty.KeyFrame>
                {
                    new(baseScale, 0f),
                    new(baseScale * 1.5f, length),
                }, AnimationInterpolationMode.Linear),
            },
        };
    }

    private static AnimationTrackComponentProperty ColorTrack(List<AnimationTrackProperty.KeyFrame> frames, AnimationInterpolationMode mode)
        => Track(nameof(SpriteComponent.Color), frames, mode);

    private static AnimationTrackComponentProperty ScaleTrack(List<AnimationTrackProperty.KeyFrame> frames, AnimationInterpolationMode mode)
        => Track(nameof(SpriteComponent.Scale), frames, mode);

    private static AnimationTrackComponentProperty OffsetTrack(List<AnimationTrackProperty.KeyFrame> frames, AnimationInterpolationMode mode)
        => Track(nameof(SpriteComponent.Offset), frames, mode);

    private static AnimationTrackComponentProperty RotationTrack(List<AnimationTrackProperty.KeyFrame> frames)
        => Track(nameof(SpriteComponent.Rotation), frames, AnimationInterpolationMode.Linear);

    private static AnimationTrackComponentProperty Track(string property, List<AnimationTrackProperty.KeyFrame> frames, AnimationInterpolationMode mode)
        => new()
        {
            ComponentType = typeof(SpriteComponent),
            Property = property,
            InterpolationMode = mode,
            KeyFrames = frames,
        };
}
