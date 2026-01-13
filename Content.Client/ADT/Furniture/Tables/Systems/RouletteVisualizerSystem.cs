using Content.Shared.ADT.Furniture.Tables.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Furniture.Tables.Systems;

public sealed class RouletteVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const string AnimationKey = "roulette_roll";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RouletteComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, RouletteComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData(uid, RouletteVisuals.State, out RouletteState state))
            state = RouletteState.Idle;

        if (!_appearance.TryGetData(uid, RouletteVisuals.Result, out int result))
            result = 0;

        switch (state)
        {
            case RouletteState.Idle:
                args.Sprite.LayerSetState(RouletteVisualLayers.Base, "idle");
                break;

            case RouletteState.Rolling:
                if (!_animation.HasRunningAnimation(uid, AnimationKey))
                {
                    PlayRollAnimation(uid, args.Sprite);
                }
                break;

            case RouletteState.Result:
                _animation.Stop(uid, AnimationKey);
                args.Sprite.LayerSetState(RouletteVisualLayers.Base, "idle");
                break;
        }
    }

    private void PlayRollAnimation(EntityUid uid, SpriteComponent sprite)
    {
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(9.0),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = RouletteVisualLayers.Base,
                    KeyFrames =
                    {
                        // Fast spinning (0-6 seconds, ~30 keyframes at 0.2s intervals)
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.00f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.30f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.35f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.40f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.50f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.60f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.65f),
                        new AnimationTrackSpriteFlick.KeyFrame("roll", 0.20f),
                    }
                }
            }
        };

        _animation.Play(uid, animation, AnimationKey);
    }
}
