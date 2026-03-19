using Content.Shared.ADT.Predictor;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Predictor;

public sealed class PredictorMachineSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PredictorMachineComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<PredictorMachineComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnAnimationCompleted(EntityUid uid, PredictorMachineComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !_appearanceSystem.TryGetData<PredictorMachineState>(uid, PredictorMachineVisuals.State, out var visualState, appearance))
        {
            visualState = PredictorMachineState.Off;
        }

        UpdateAppearance(uid, visualState, component, sprite);
    }

    private void OnAppearanceChange(EntityUid uid, PredictorMachineComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(PredictorMachineVisuals.State, out var visualStateObject) ||
            visualStateObject is not PredictorMachineState visualState)
        {
            visualState = PredictorMachineState.Off;
        }

        UpdateAppearance(uid, visualState, component, args.Sprite);
    }

    private void UpdateAppearance(EntityUid uid, PredictorMachineState visualState, PredictorMachineComponent component, SpriteComponent sprite)
    {
        switch (visualState)
        {
            case PredictorMachineState.Off:
                SetLayerState(component.OffState, (uid, sprite));
                break;

            case PredictorMachineState.On:
                SetLayerState(component.OnState, (uid, sprite));
                break;

            case PredictorMachineState.Predicting:
                var animationTime = 2.0f * component.AnimationCycles;
                PlayAnimation(uid, component.AnimationState, animationTime, sprite);
                break;

            case PredictorMachineState.EmaggedOn:
                SetLayerState(component.EmaggedState, (uid, sprite));
                break;

            case PredictorMachineState.EmaggedPredicting:
                var emaggedAnimationTime = 2.0f * component.AnimationCycles;
                PlayAnimation(uid, component.EmaggedAnimationState, emaggedAnimationTime, sprite);
                break;
        }
    }

    private void SetLayerState(string? state, Entity<SpriteComponent> sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        _sprite.LayerSetAutoAnimated(sprite.AsNullable(), 0, true);
        _sprite.LayerSetRsiState(sprite.AsNullable(), 0, state);
    }

    private void PlayAnimation(EntityUid uid, string? state, float animationTime, SpriteComponent sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        if (!_animationPlayer.HasRunningAnimation(uid, state))
        {
            _sprite.LayerSetAutoAnimated((uid, sprite), 0, true);
            _sprite.LayerSetRsiState((uid, sprite), 0, state);

            Timer.Spawn(TimeSpan.FromSeconds(animationTime), () =>
            {
                if (Deleted(uid) || !TryComp<SpriteComponent>(uid, out var s))
                    return;

                if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
                    !_appearanceSystem.TryGetData<PredictorMachineState>(uid, PredictorMachineVisuals.State, out var visualState, appearance))
                {
                    visualState = PredictorMachineState.Off;
                }

                if (visualState == PredictorMachineState.Predicting || visualState == PredictorMachineState.EmaggedPredicting)
                {
                    if (TryComp<PredictorMachineComponent>(uid, out var comp) && !comp.IsAnimating)
                    {
                        var onState = visualState == PredictorMachineState.EmaggedPredicting ? comp.EmaggedState : comp.OnState;
                        _sprite.LayerSetRsiState((uid, s), 0, onState);
                    }
                }
            });
        }
    }
}
