using Content.Shared.ADT.Implants;
using Robust.Client.GameObjects;
using Content.Shared.Body.Components;
using System.Diagnostics;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Implants;

public sealed class VisibleImplantSystem : SharedVisibleImplantSystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MantisDaggersComponent, AppearanceChangeEvent>(HandleDaggers);
        SubscribeLocalEvent<MantisDaggersComponent, ComponentShutdown>(OnDaggersShutdown);
        SubscribeLocalEvent<MistralFistsComponent, AppearanceChangeEvent>(HandleFists);
        SubscribeLocalEvent<SundownerShieldsComponent, AppearanceChangeEvent>(HandleShields);
    }

    public void HandleDaggers(EntityUid uid, MantisDaggersComponent comp, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _appearance.TryGetData<bool>(uid, MantisDaggersVisuals.Left, out var left);
        _appearance.TryGetData<bool>(uid, MantisDaggersVisuals.Right, out var right);

        SetDaggerLayer(sprite, comp.LeftLayerMap, comp.LeftSprite, left);
        SetDaggerLayer(sprite, comp.RightLayerMap, comp.RightSprite, right);
    }

    private void OnDaggersShutdown(Entity<MantisDaggersComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (sprite.LayerMapTryGet(ent.Comp.LeftLayerMap, out var left))
            sprite.LayerSetVisible(left, false);

        if (sprite.LayerMapTryGet(ent.Comp.RightLayerMap, out var right))
            sprite.LayerSetVisible(right, false);
    }

    private void SetDaggerLayer(SpriteComponent sprite, string layerMap, SpriteSpecifier? daggerSprite, bool visible)
    {
        if (daggerSprite is null)
            return;

        if (!sprite.LayerMapTryGet(layerMap, out var index))
            index = sprite.LayerMapReserveBlank(layerMap);

        sprite.LayerSetSprite(index, daggerSprite);
        sprite.LayerSetVisible(index, visible);
    }

    public void HandleFists(EntityUid uid, MistralFistsComponent comp, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (comp.FallbackSprite is null)
            return;

        if (!sprite.LayerMapTryGet(comp.LayerMap, out var index))
            index = sprite.LayerMapReserveBlank(comp.LayerMap);

        sprite.LayerSetSprite(index, comp.FallbackSprite);

        _appearance.TryGetData<bool>(uid, MistralFistsVisuals.Active, out var active);
        sprite.LayerSetVisible(index, active);
    }

    public void HandleShields(EntityUid uid, SundownerShieldsComponent comp, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (comp.FallbackSprite is null)
            return;

        if (!sprite.LayerMapTryGet(comp.LayerMap, out var index))
            index = sprite.LayerMapReserveBlank(comp.LayerMap);

        if (_appearance.TryGetData<bool>(uid, SundownerShieldsVisuals.Closed, out var closed) &&
            _appearance.TryGetData<bool>(uid, SundownerShieldsVisuals.Open, out var open))
        {
            if (closed)
                sprite.LayerSetSprite(index, comp.FallbackSpriteClosed);
            else
                sprite.LayerSetSprite(index, comp.FallbackSprite);
        }
    }
}
