using System.Numerics;
using Content.Shared.ADT.Xenobiology;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Xenobiology;

/// <summary>
/// Plays the slime biting animation when a slime latches onto something.
/// </summary>
public sealed partial class SlimeAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string SlimeEatAnimationKey = "slime-eat";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SlimeBiteAnimationMessage>(OnSlimeBiteAnimation);
    }

    private void OnSlimeBiteAnimation(SlimeBiteAnimationMessage args)
    {
        var entityUid = GetEntity(args.Entity);
        if (_animation.HasRunningAnimation(entityUid, SlimeEatAnimationKey))
            _animation.Stop(entityUid, SlimeEatAnimationKey);
        _animation.Play(entityUid, GetSlimeEatAnimation(args.Angle), SlimeEatAnimationKey);
    }

    private Animation GetSlimeEatAnimation(Angle rot)
    {
        const float Distance = 0.15f;
        const float Length = 0.15f;
        var startOffset = Vector2.Zero;
        var endOffset = rot.RotateVec(new Vector2(0f, -Distance));

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(Length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startOffset, 0F),
                        new AnimationTrackProperty.KeyFrame(endOffset, 0.05F),
                        new AnimationTrackProperty.KeyFrame(startOffset, Length),
                    }
                },
            }
        };
    }
}
