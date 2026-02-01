using Content.Shared.VendingMachines;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using System.Linq;

namespace Content.Client.VendingMachines;

public sealed class VendingMachineSystem : SharedVendingMachineSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<VendingMachineComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<VendingMachineComponent, ComponentHandleState>(OnVendingHandleState);
    }

    private void OnVendingHandleState(EntityUid uid, VendingMachineComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not VendingMachineComponentState state)
            return;

        component.Contraband = state.Contraband;
        component.EjectEnd = state.EjectEnd;
        component.DenyEnd = state.DenyEnd;
        component.DispenseOnHitEnd = state.DispenseOnHitEnd;
        component.Broken = state.Broken;

        var fullUiUpdate = !component.Inventory.Keys.SequenceEqual(state.Inventory.Keys) ||
                           !component.EmaggedInventory.Keys.SequenceEqual(state.EmaggedInventory.Keys) ||
                           !component.ContrabandInventory.Keys.SequenceEqual(state.ContrabandInventory.Keys);

        component.Inventory = new Dictionary<string, VendingMachineInventoryEntry>(state.Inventory);
        component.EmaggedInventory = new Dictionary<string, VendingMachineInventoryEntry>(state.EmaggedInventory);
        component.ContrabandInventory = new Dictionary<string, VendingMachineInventoryEntry>(state.ContrabandInventory);

    }

    private void OnAnimationCompleted(EntityUid uid, VendingMachineComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!component.Broken && component.EjectEnd == null && component.DenyEnd == null)
        {
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                _appearanceSystem.SetData(uid, VendingMachineVisuals.VisualState, VendingMachineVisualState.Normal, appearance);
            }
        }
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
                {
                    SetLayerState(VendingMachineVisualLayers.BaseUnshaded, component.DenyState, (uid, sprite));
                }
                else
                {
                    PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, component.DenyState,
                        (float)component.DenyDelay.TotalSeconds, sprite);
                }

                SetLayerState(VendingMachineVisualLayers.Screen, component.ScreenState, (uid, sprite));
                break;

            case VendingMachineVisualState.Eject:
                PlayAnimation(uid, VendingMachineVisualLayers.BaseUnshaded, component.EjectState,
                    (float)component.EjectDelay.TotalSeconds, sprite);

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

        var animName = $"vend_{layer}_{state}";

        if (_animationPlayer.HasRunningAnimation(uid, animName))
            return;

        var animation = new Animation
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

        _sprite.LayerSetVisible((uid, sprite), layer, true);
        _animationPlayer.Play(uid, animation, animName);
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