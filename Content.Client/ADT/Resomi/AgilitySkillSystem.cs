using System.Numerics;
using Content.Shared.ADT.Resomi.Abilities;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client.ADT.Resomi.Abilities;

public sealed class AgillitySkillSystem : SharedAgillitySkillSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    public override void DoJumpEffect(Entity<AgillitySkillComponent> ent)
    {
        base.DoJumpEffect(ent);
        if (!Timing.IsFirstTimePredicted)
            return;
        var animationKey = "agilityJump";

        if (_animation.HasRunningAnimation(ent.Owner, animationKey))
            return;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(250),
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
                        new AnimationTrackProperty.KeyFrame(new Vector2(0, 0.75f), 0.125f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0.125f),
                    }
                }
            }
        };

        _animation.Play(ent, animation, animationKey);
    }
}
