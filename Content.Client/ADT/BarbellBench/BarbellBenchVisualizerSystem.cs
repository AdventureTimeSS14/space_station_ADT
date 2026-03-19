using Content.Shared.ADT.BarbellBench;
using Content.Shared.ADT.BarbellBench.Components;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared.Buckle.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client.ADT.BarbellBench;

public sealed class BarbellBenchVisualizerSystem : VisualizerSystem<BarbellBenchVisualsComponent>
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const string OverlayLayerKey = "barbell-overlay";
    private const string OverlayAnimationKey = "barbell_rep_overlay_animation";

    protected override void OnAppearanceChange(EntityUid uid, BarbellBenchVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<BarbellBenchState>(uid, BarbellBenchVisuals.State, out var state, args.Component))
            state = BarbellBenchState.Idle;

        if (!TryComp<BarbellBenchComponent>(uid, out var benchComp))
            return;

        var overlayUid = benchComp.OverlayEntity;
        var slotId = benchComp.BarbellSlotId;

        var hasAttachableHolder = HasComp<AttachableHolderComponent>(uid);
        EntityUid? attachedBarbell = null;
        if (hasAttachableHolder && _container.TryGetContainer(uid, slotId, out var slotContainer) && slotContainer.Count > 0)
            attachedBarbell = slotContainer.ContainedEntities[0];
        var hasBarbellAttached = attachedBarbell != null;

        ResPath? overlayRsi = null;
        if (attachedBarbell != null && TryComp<BarbellLiftComponent>(attachedBarbell.Value, out var lift))
            overlayRsi = lift.OverlayRsi;

        var isBuckled = TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count > 0;

        if (overlayUid != null)
            _animationPlayer.Stop(overlayUid.Value, OverlayAnimationKey);

        if (hasAttachableHolder && SpriteSystem.LayerMapTryGet((uid, args.Sprite), slotId, out var attachedLayer, false))
        {
            var visible = hasBarbellAttached && !isBuckled && overlayRsi != null;
            SpriteSystem.LayerSetVisible((uid, args.Sprite), attachedLayer, visible);
            if (visible)
            {
                SpriteSystem.LayerSetData((uid, args.Sprite), attachedLayer, new PrototypeLayerData
                {
                    RsiPath = overlayRsi!.Value.ToString(),
                    State = component.AttachedState,
                    Offset = Vector2.Zero,
                    Visible = true
                });
            }
        }

        if (overlayUid == null || !TryComp<SpriteComponent>(overlayUid.Value, out var overlaySpriteComp))
            return;

        var overlayEnt = overlayUid.Value;
        if (!SpriteSystem.LayerMapTryGet((overlayEnt, overlaySpriteComp), OverlayLayerKey, out var overlayLayer, false))
        {
            SpriteSystem.LayerMapSet((overlayEnt, overlaySpriteComp), OverlayLayerKey, 0);
            overlayLayer = 0;
        }

        var showOverlay = hasBarbellAttached && isBuckled;

        if (!showOverlay)
        {
            SpriteSystem.LayerSetVisible((overlayEnt, overlaySpriteComp), overlayLayer, false);
            SpriteSystem.LayerSetAutoAnimated((overlayEnt, overlaySpriteComp), overlayLayer, false);
            if (overlayRsi != null)
            {
                SpriteSystem.LayerSetData((overlayEnt, overlaySpriteComp), overlayLayer, new PrototypeLayerData
                {
                    RsiPath = overlayRsi.Value.ToString(),
                    State = component.OverlayBaseState,
                    Offset = Vector2.Zero,
                    Visible = false
                });
            }
            return;
        }

        SpriteSystem.LayerSetVisible((overlayEnt, overlaySpriteComp), overlayLayer, true);

        if (overlayRsi != null)
        {
            SpriteSystem.LayerSetData((overlayEnt, overlaySpriteComp), overlayLayer, new PrototypeLayerData
            {
                RsiPath = overlayRsi.Value.ToString(),
                State = component.OverlayBaseState,
                Offset = Vector2.Zero,
                Visible = true
            });
        }
        else
        {
            SpriteSystem.LayerSetRsiState((overlayEnt, overlaySpriteComp), overlayLayer, component.OverlayBaseState);
        }

        if (state == BarbellBenchState.PerformingRep)
        {
            var overlayAnimation = new Animation
            {
                Length = TimeSpan.FromSeconds(benchComp.RepDuration),
                AnimationTracks =
                {
                    new AnimationTrackSpriteFlick
                    {
                        LayerKey = OverlayLayerKey,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(component.OverlayRepFlickState, 0f)
                        }
                    }
                }
            };

            _animationPlayer.Play(overlayEnt, overlayAnimation, OverlayAnimationKey);
        }
        else
        {
            SpriteSystem.LayerSetAutoAnimated((overlayEnt, overlaySpriteComp), overlayLayer, false);
        }
    }
}
