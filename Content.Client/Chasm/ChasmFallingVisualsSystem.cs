using Content.Shared.Chasm;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Random; //ADT-Tweak

namespace Content.Client.Chasm;

/// <summary>
///     Handles the falling animation for entities that fall into a chasm.
/// </summary>
public sealed class ChasmFallingVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IRobustRandom _random = default!; //ADT-Tweak

    private readonly string _chasmFallAnimationKey = "chasm_fall";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmFallingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ChasmFallingComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(EntityUid uid, ChasmFallingComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            TerminatingOrDeleted(uid))
        {
            return;
        }

        component.OriginalScale = sprite.Scale;

        if (!TryComp<AnimationPlayerComponent>(uid, out var player))
            return;

        if (_anim.HasRunningAnimation(player, _chasmFallAnimationKey))
            return;

        _anim.Play((uid, player), GetFallingAnimation(component), _chasmFallAnimationKey);
    }

    private void OnComponentRemove(EntityUid uid, ChasmFallingComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetScale((uid, sprite), component.OriginalScale);

        if (!TryComp<AnimationPlayerComponent>(uid, out var player))
            return;

        if (_anim.HasRunningAnimation(player, _chasmFallAnimationKey))
            _anim.Stop((uid, player), _chasmFallAnimationKey);
    }

    private Animation GetFallingAnimation(ChasmFallingComponent component)
    {
        var length = component.AnimationTime;
        //ADT-Tweak-Start
        var direction = _random.Prob(0.5f) ? 1 : -1;
        var totalRotation = _random.NextFloat(360f, 720f) * direction;
        //ADT-Tweak-End

        return new Animation()
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(component.OriginalScale, 0.0f),
                        new AnimationTrackProperty.KeyFrame(component.AnimationScale, length.Seconds),
                    },
                    InterpolationMode = AnimationInterpolationMode.Cubic
                //ADT-Tweak-Start
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(totalRotation), length.Seconds),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                //ADT-Tweak-End
                }
            }
        };
    }
}
