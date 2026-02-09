using Content.Shared.VendingMachines;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.VendingMachines;

public sealed class VendingMachineSystem : SharedVendingMachineSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<VendingMachineComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<VendingMachineComponent, AfterAutoHandleStateEvent>(OnVendingAfterState);
    }

    private void OnVendingAfterState(EntityUid uid, VendingMachineComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_uiSystem.TryGetOpenUi<VendingMachineBoundUserInterface>(uid, VendingMachineUiKey.Key, out var bui))
        {
            bui.Refresh();
        }
    }

    private void OnAnimationCompleted(EntityUid uid, VendingMachineComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !_appearanceSystem.TryGetData<VendingMachineVisualState>(uid, VendingMachineVisuals.VisualState, out var visualState, appearance))
        {
            visualState = VendingMachineVisualState.Normal;
        }

        UpdateAppearance(uid, visualState, component, sprite);
    }

    private void OnAppearanceChange(EntityUid uid, VendingMachineComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(VendingMachineVisuals.VisualState, out var visualStateObject) ||
            visualStateObject is not VendingMachineVisualState visualState)
        {
            visualState = VendingMachineVisualState.Normal;
        }

        UpdateAppearance(uid, visualState, component, args.Sprite);
    }

    private void UpdateAppearance(EntityUid uid, VendingMachineVisualState visualState, VendingMachineComponent component, SpriteComponent sprite)
    {
        SetLayerState(VendingMachineVisualLayers.Base, component.OffState, (uid, sprite));

        switch (visualState)
        {
            case VendingMachineVisualState.Normal:
                SetLayerState(VendingMachineVisualLayers.BaseUnshaded, component.NormalState, (uid, sprite));
                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Deny:
                if (component.LoopDenyAnimation)
                    SetLayerState(VendingMachineVisualLayers.BaseUnshaded, component.DenyState, (uid, sprite));
                else
                    PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, component.DenyState, component.DenyDelay, sprite);

                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Eject:
                PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, component.EjectState, (float)component.EjectDelay, sprite); //ADT tweaked
                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Broken:
                HideLayers((uid, sprite));
                SetLayerState(VendingMachineVisualLayers.Base, component.BrokenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Off:
                HideLayers((uid, sprite));
                break;
        }
    }

    private void SetLayerState(VendingMachineVisualLayers layer, string? state, Entity<SpriteComponent> sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        _sprite.LayerSetVisible(sprite.AsNullable(), layer, true);
        _sprite.LayerSetAutoAnimated(sprite.AsNullable(), layer, true);
        _sprite.LayerSetRsiState(sprite.AsNullable(), layer, state);
    }

    private void PlayAnimation(EntityUid uid, VendingMachineVisualLayers layer, string? state, float animationTime, SpriteComponent sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        if (!_animationPlayer.HasRunningAnimation(uid, state))
        {
            var animation = GetAnimation(layer, state, animationTime);
            _sprite.LayerSetVisible((uid, sprite), layer, true);
            _animationPlayer.Play(uid, animation, state);
        }
    }

    private static Animation GetAnimation(VendingMachineVisualLayers layer, string state, float animationTime)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(animationTime),
            AnimationTracks =
                {
                    new AnimationTrackSpriteFlick
                    {
                        LayerKey = layer,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(state, 0f)
                        }
                    }
                }
        };
    }

    private void HideLayers(Entity<SpriteComponent> sprite)
    {
        HideLayer(VendingMachineVisualLayers.BaseUnshaded, sprite);
        HideLayer(VendingMachineVisualLayers.Screen, sprite);
    }

    private void HideLayer(VendingMachineVisualLayers layer, Entity<SpriteComponent> sprite)
    {
        if (!_sprite.LayerMapTryGet(sprite.AsNullable(), layer, out var actualLayer, false))
            return;

        _sprite.LayerSetVisible(sprite.AsNullable(), actualLayer, false);
    }
}
