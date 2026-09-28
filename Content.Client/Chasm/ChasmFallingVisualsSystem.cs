using Content.Shared.Chasm;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Random; //ADT-Tweak

namespace Content.Client.Chasm;

/// <summary>
/// Handles the falling animation for entities that fall into an entity with <see cref="ChasmComponent"/>.
/// </summary>
public sealed partial class ChasmFallingVisualsSystem : EntitySystem
{
<<<<<<< HEAD
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IRobustRandom _random = default!; //ADT-Tweak
=======
    [Dependency] private AnimationPlayerSystem _anim = default!;
    [Dependency] private SpriteSystem _sprite = default!;
>>>>>>> wizards-filtered

    [Dependency] private EntityQuery<AnimationPlayerComponent> _animationPlayerQuery;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    private const string ChasmFallAnimationKey = "chasm_fall";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChasmFallingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ChasmFallingComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentInit(Entity<ChasmFallingComponent> entity, ref ComponentInit args)
    {
        if (!_spriteQuery.TryComp(entity, out var sprite) ||
            TerminatingOrDeleted(entity))
        {
            return;
        }

        entity.Comp.OriginalScale = sprite.Scale;

<<<<<<< HEAD
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
=======
        if (!_animationPlayerQuery.TryComp(entity, out var player) ||
            _anim.HasRunningAnimation(player, ChasmFallAnimationKey))
>>>>>>> wizards-filtered
        {
            return;
        }

        _anim.Play((entity, player), GetFallingAnimation(entity.Comp), ChasmFallAnimationKey);
    }

    private void OnComponentRemove(Entity<ChasmFallingComponent> entity, ref ComponentRemove args)
    {
        if (!_spriteQuery.TryComp(entity, out var sprite))
        {
            return;
        }

        _sprite.SetScale((entity, sprite), entity.Comp.OriginalScale);

        if (!_animationPlayerQuery.TryComp(entity, out var player) ||
            !_anim.HasRunningAnimation(player, ChasmFallAnimationKey))
        {
            return;
        }

        _anim.Stop((entity, player), ChasmFallAnimationKey);
    }

    private static Animation GetFallingAnimation(ChasmFallingComponent component)
    {
        return new Animation
        {
            Length = component.AnimationTime,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(component.OriginalScale, 0.0f),
                        new AnimationTrackProperty.KeyFrame(component.AnimationScale, component.AnimationTime.Seconds),
                    },
<<<<<<< HEAD
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
=======
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                },
            },
>>>>>>> wizards-filtered
        };
    }
}
