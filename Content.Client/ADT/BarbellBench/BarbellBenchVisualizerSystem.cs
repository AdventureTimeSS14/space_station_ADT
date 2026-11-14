using Content.Shared.ADT.BarbellBench;
using Content.Shared.ADT.BarbellBench.Components;
using Content.Shared.Buckle.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client.ADT.BarbellBench;

public sealed class BarbellBenchVisualizerSystem : VisualizerSystem<BarbellBenchVisualsComponent>
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    private const string OverlayLayerKey = "barbell-overlay";
    private const string OverlayAnimationKey = "barbell_rep_overlay_animation";

    private readonly Dictionary<EntityUid, EntityUid> _overlays = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarbellBenchVisualsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BarbellBenchVisualsComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<BarbellBenchVisualsComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInserted(Entity<BarbellBenchVisualsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        OnSlotChanged(ent, args.Container);
    }

    private void OnRemoved(Entity<BarbellBenchVisualsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        OnSlotChanged(ent, args.Container);
    }

    private void OnSlotChanged(Entity<BarbellBenchVisualsComponent> ent, BaseContainer container)
    {
        if (!TryComp<BarbellBenchComponent>(ent, out var bench) || container.ID != bench.BarbellSlotId)
            return;

        if (TryComp<AppearanceComponent>(ent, out var appearance))
            AppearanceSystem.QueueUpdate(ent, appearance);
    }

    private void OnShutdown(Entity<BarbellBenchVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (_overlays.Remove(ent.Owner, out var overlay))
            QueueDel(overlay);
    }

    private EntityUid EnsureOverlay(EntityUid bench, BarbellBenchComponent comp)
    {
        if (_overlays.TryGetValue(bench, out var existing) && !TerminatingOrDeleted(existing))
            return existing;

        var overlay = SpawnAttachedTo(comp.OverlayPrototype, new EntityCoordinates(bench, Vector2.Zero));
        _overlays[bench] = overlay;
        return overlay;
    }

    protected override void OnAppearanceChange(EntityUid uid, BarbellBenchVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<BarbellBenchState>(uid, BarbellBenchVisuals.State, out var state, args.Component))
            state = BarbellBenchState.Idle;

        if (!TryComp<BarbellBenchComponent>(uid, out var benchComp))
            return;

        EntityUid? overlayUid = EnsureOverlay(uid, benchComp);
        var slotId = benchComp.BarbellSlotId;

        EntityUid? attachedBarbell = null;
        if (_container.TryGetContainer(uid, slotId, out var slotContainer) && slotContainer.Count > 0)
            attachedBarbell = slotContainer.ContainedEntities[0];
        var hasBarbellAttached = attachedBarbell != null;

        ResPath? overlayRsi = null;
        if (attachedBarbell != null && TryComp<BarbellLiftComponent>(attachedBarbell.Value, out var lift))
            overlayRsi = lift.OverlayRsi;

        var isBuckled = TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count > 0;

        if (overlayUid != null)
            _animationPlayer.Stop(overlayUid.Value, OverlayAnimationKey);

        if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), slotId, out var attachedLayer, false))
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
