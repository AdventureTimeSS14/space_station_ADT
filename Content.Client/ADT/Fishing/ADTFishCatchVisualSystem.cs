using System.Numerics;
using Content.Shared.ADT.Fishing;
using Content.Shared.ADT.Fishing.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.ADT.Fishing;

public sealed class ADTFishCatchVisualSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string AnimationKey = "adt-fish-catch";
    private const string EffectPrototype = "ADTFishCatchEffect";
    private const float Length = 0.6f;
    private const float ArcHeight = 0.8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ADTFishCaughtEvent>(OnFishCaught);
        SubscribeLocalEvent<ADTFishCatchEffectComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnFishCaught(ADTFishCaughtEvent ev)
    {
        var user = GetEntity(ev.User);

        if (!Exists(user))
            return;

        var from = _transform.ToMapCoordinates(GetCoordinates(ev.From));
        var to = _transform.GetMapCoordinates(user);

        if (from.MapId != to.MapId)
            return;

        var effect = Spawn(EffectPrototype, from);

        if (!TryComp<SpriteComponent>(effect, out var sprite))
        {
            QueueDel(effect);
            return;
        }

        var texture = _sprite.GetPrototypeIcon(ev.Fish).Default;
        _sprite.AddTextureLayer((effect, sprite), texture);

        var invRotation = -_transform.GetWorldRotation(effect);
        var delta = invRotation.RotateVec(to.Position - from.Position);
        var up = invRotation.RotateVec(new Vector2(0f, ArcHeight));

        _animation.Play(effect, GetAnimation(delta, up), AnimationKey);
    }

    private void OnAnimationCompleted(Entity<ADTFishCatchEffectComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        QueueDel(ent);
    }

    private static Animation GetAnimation(Vector2 delta, Vector2 up)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(Length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(delta * 0.5f + up, Length * 0.5f),
                        new AnimationTrackProperty.KeyFrame(delta, Length * 0.5f),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(0.4f, 0.4f), 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1.6f, 1.6f), Length * 0.6f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(1f, 1f), Length * 0.4f),
                    },
                },
            },
        };
    }
}
