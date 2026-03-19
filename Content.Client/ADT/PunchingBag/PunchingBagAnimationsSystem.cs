using Content.Shared.ADT.PunchingBag;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.ADT.PunchingBag;

public sealed class PunchingBagAnimationsSystem : SharedPunchingBagAnimationsSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string BaseLayerKey = "base";
    private const string AnimationKey = "punching-bag-animation";

    private readonly Dictionary<string, Animation> _animationCache = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<PunchingBagAnimationEvent>(ev =>
            PlayAnimation(GetEntity(ev.Uid), EntityUid.Invalid, ev.AnimationState));
    }

    private Animation GetOrCreateAnimation(string animationState)
    {
        if (_animationCache.TryGetValue(animationState, out var cached))
            return cached;

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(1.0),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = BaseLayerKey,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(animationState, 0f),
                        new AnimationTrackSpriteFlick.KeyFrame("punchingbag", 1.0f),
                    }
                }
            }
        };

        _animationCache[animationState] = animation;
        return animation;
    }

    protected override void PlayAnimation(EntityUid uid, EntityUid attacker, string animationState)
    {
        if (!_timing.IsFirstTimePredicted && attacker != EntityUid.Invalid)
            return;

        if (TerminatingOrDeleted(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_animationSystem.HasRunningAnimation(uid, AnimationKey))
            _animationSystem.Stop(uid, AnimationKey);

        if (!_sprite.LayerMapTryGet((uid, sprite), BaseLayerKey, out _, false))
            _sprite.LayerMapSet((uid, sprite), BaseLayerKey, 0);

        _animationSystem.Play(uid, GetOrCreateAnimation(animationState), AnimationKey);
    }
}
