using Content.Shared.ADT.Drake;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Maths;

namespace Content.Client.ADT.Drake;

public sealed class ADTDrakeVisualEffectSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;

    private const string AnimationKey = "adt-drake-visual-effect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDrakeVisualEffectComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<ADTDrakeVisualEffectComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Keyframes.Count == 0)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var offsets = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Offset),
            InterpolationMode = AnimationInterpolationMode.Linear,
        };

        var colors = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Color),
            InterpolationMode = AnimationInterpolationMode.Linear,
        };

        var scales = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Scale),
            InterpolationMode = AnimationInterpolationMode.Linear,
        };

        var states = new AnimationTrackSpriteFlick
        {
            LayerKey = ent.Comp.LayerKey,
        };

        var offset = sprite.Offset;
        var color = sprite.Color;
        var scale = sprite.Scale;
        var length = 0f;
        var sinceState = 0f;

        foreach (var frame in ent.Comp.Keyframes)
        {
            length += frame.Time;
            sinceState += frame.Time;

            if (frame.State is { } state)
            {
                states.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(state, sinceState));
                sinceState = 0f;
            }

            if (frame.Offset is { } newOffset)
                offset = newOffset;

            if (frame.Alpha is { } alpha)
                color = color.WithAlpha(alpha);

            if (frame.Scale is { } newScale)
                scale = newScale;

            var easing = GetEasing(frame.Easing);

            offsets.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(offset, frame.Time, easing));
            colors.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(color, frame.Time, easing));
            scales.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(scale, frame.Time, easing));
        }

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                offsets,
                colors,
                scales,
            },
        };

        if (states.KeyFrames.Count > 0)
            animation.AnimationTracks.Add(states);

        _animationPlayer.Play(ent.Owner, animation, AnimationKey);
    }

    public static Func<float, float>? GetEasing(ADTDrakeEasing easing)
    {
        return easing switch
        {
            ADTDrakeEasing.OutBounce => new Func<float, float>(Easings.OutBounce),
            _ => null,
        };
    }
}
