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

        AppearanceSystem.TryGetData<bool>(uid, BarbellBenchVisuals.HasBarbell, out _, args.Component);

        if (!TryComp<BarbellBenchComponent>(uid, out var benchComp))
            return;

        var overlayUid = benchComp.OverlayEntity;
        var slotId = benchComp.BarbellSlotId;

        var hasAttachableHolder = TryComp<AttachableHolderComponent>(uid, out _);
        EntityUid? attachedBarbell = null;
        if (hasAttachableHolder && _container.TryGetContainer(uid, slotId, out var slotContainer) && slotContainer.Count > 0)
            attachedBarbell = slotContainer.ContainedEntities[0];
        var hasBarbellAttached = attachedBarbell != null;

        ResPath? overlayRsi = null;
        if (attachedBarbell != null && TryComp<BarbellLiftComponent>(attachedBarbell.Value, out var lift))
            overlayRsi = lift.OverlayRsi;

        var attachedState = component.AttachedState;
        var overlayBaseState = component.OverlayBaseState;
        var overlayRepFlickState = component.OverlayRepFlickState;

        var isBuckled = TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count > 0;

        if (overlayUid != null)
            _animationPlayer.Stop(overlayUid.Value, OverlayAnimationKey);

        if (hasAttachableHolder && SpriteSystem.LayerMapTryGet((uid, args.Sprite), slotId, out var attachedLayer, false))
        {
            var visible = hasBarbellAttached && !isBuckled && overlayRsi != null;
            SpriteSystem.LayerSetVisible((uid, args.Sprite), attachedLayer, visible);
            if (visible)
            {
                var layerData = new PrototypeLayerData
                {
                    RsiPath = overlayRsi!.Value.ToString(),
                    State = attachedState,
                    Offset = Vector2.Zero,
                    Visible = true
                };
                SpriteSystem.LayerSetData((uid, args.Sprite), attachedLayer, layerData);
            }
        }

        if (overlayUid != null &&
            TryComp<SpriteComponent>(overlayUid.Value, out var overlaySpriteCompDefault))
        {
            var overlayEnt = overlayUid.Value;
            if (!SpriteSystem.LayerMapTryGet((overlayEnt, overlaySpriteCompDefault), OverlayLayerKey, out var overlayLayer, false))
            {
                SpriteSystem.LayerMapSet((overlayEnt, overlaySpriteCompDefault), OverlayLayerKey, 0);
                overlayLayer = 0;
            }

            SpriteSystem.LayerSetVisible((overlayEnt, overlaySpriteCompDefault), overlayLayer, false);
            SpriteSystem.LayerSetAutoAnimated((overlayEnt, overlaySpriteCompDefault), overlayLayer, false);

            if (overlayRsi != null)
            {
                var layerData = new PrototypeLayerData
                {
                    RsiPath = overlayRsi.Value.ToString(),
                    State = overlayBaseState,
                    Offset = Vector2.Zero,
                    Visible = false
                };
                SpriteSystem.LayerSetData((overlayEnt, overlaySpriteCompDefault), overlayLayer, layerData);
            }
        }

        switch (state)
        {
            case BarbellBenchState.PerformingRep:
                if (hasBarbellAttached &&
                    isBuckled &&
                    overlayUid != null &&
                    TryComp<SpriteComponent>(overlayUid.Value, out var overlaySpriteComp))
                {
                    var overlayEnt = overlayUid.Value;
                    if (!SpriteSystem.LayerMapTryGet((overlayEnt, overlaySpriteComp), OverlayLayerKey, out var overlayLayer, false))
                    {
                        SpriteSystem.LayerMapSet((overlayEnt, overlaySpriteComp), OverlayLayerKey, 0);
                        overlayLayer = 0;
                    }

                    SpriteSystem.LayerSetVisible((overlayEnt, overlaySpriteComp), overlayLayer, true);
                    if (overlayRsi != null)
                    {
                        var layerData = new PrototypeLayerData
                        {
                            RsiPath = overlayRsi.Value.ToString(),
                            State = overlayBaseState,
                            Offset = Vector2.Zero,
                            Visible = true
                        };
                        SpriteSystem.LayerSetData((overlayEnt, overlaySpriteComp), overlayLayer, layerData);
                    }
                    else
                    {
                        SpriteSystem.LayerSetRsiState((overlayEnt, overlaySpriteComp), overlayLayer, overlayBaseState);
                    }

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
                                    new AnimationTrackSpriteFlick.KeyFrame(overlayRepFlickState, 0f)
                                }
                            }
                        }
                    };

                    _animationPlayer.Play(overlayEnt, overlayAnimation, OverlayAnimationKey);
                }
                break;

            case BarbellBenchState.Idle:
            default:
                if (hasBarbellAttached &&
                    isBuckled &&
                    overlayUid != null &&
                    TryComp<SpriteComponent>(overlayUid.Value, out var overlaySpriteCompIdle))
                {
                    var overlayEnt = overlayUid.Value;
                    if (!SpriteSystem.LayerMapTryGet((overlayEnt, overlaySpriteCompIdle), OverlayLayerKey, out var overlayLayer, false))
                    {
                        SpriteSystem.LayerMapSet((overlayEnt, overlaySpriteCompIdle), OverlayLayerKey, 0);
                        overlayLayer = 0;
                    }

                    SpriteSystem.LayerSetVisible((overlayEnt, overlaySpriteCompIdle), overlayLayer, true);
                    SpriteSystem.LayerSetAutoAnimated((overlayEnt, overlaySpriteCompIdle), overlayLayer, false);
                    if (overlayRsi != null)
                    {
                        var layerData = new PrototypeLayerData
                        {
                            RsiPath = overlayRsi.Value.ToString(),
                            State = overlayBaseState,
                            Offset = Vector2.Zero,
                            Visible = true
                        };
                        SpriteSystem.LayerSetData((overlayEnt, overlaySpriteCompIdle), overlayLayer, layerData);
                    }
                    else
                    {
                        SpriteSystem.LayerSetRsiState((overlayEnt, overlaySpriteCompIdle), overlayLayer, overlayBaseState);
                    }
                }
                break;
        }
    }
}
