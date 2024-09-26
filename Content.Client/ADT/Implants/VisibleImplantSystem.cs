using Content.Shared.ADT.Implants;
using Robust.Client.GameObjects;
using Content.Shared.Body.Components;
using System.Diagnostics;

namespace Content.Client.ADT.Implants;

public sealed class VisibleImplantSystem : SharedVisibleImplantSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MantisDaggersComponent, AppearanceChangeEvent>(HandleDaggers);
        SubscribeLocalEvent<MistralFistsComponent, AppearanceChangeEvent>(HandleFists);
        SubscribeLocalEvent<SundownerShieldsComponent, AppearanceChangeEvent>(HandleShields);
    }

    public void HandleDaggers(EntityUid uid, MantisDaggersComponent comp, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (comp.FallbackSprite is null)
            return;

        if (!sprite.LayerMapTryGet(comp.LayerMap, out var index))
            index = sprite.LayerMapReserveBlank(comp.LayerMap);

        sprite.LayerSetSprite(index, comp.FallbackSprite);

        _appearance.TryGetData<bool>(uid, MantisDaggersVisuals.Active, out var active);
        sprite.LayerSetVisible(index, active);
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
