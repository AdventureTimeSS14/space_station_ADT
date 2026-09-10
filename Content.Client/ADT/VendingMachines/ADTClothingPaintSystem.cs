using Content.Client.ADT.DisplacementMap;
using Content.Shared.ADT.VendingMachines;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.ADT.VendingMachines;

public sealed class ADTClothingPaintSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTClothingPaintComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<ADTClothingPaintComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<ADTClothingPaintComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    private void OnAfterHandleState(Entity<ADTClothingPaintComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var color = ent.Comp.PaintColor ?? Color.White;
        var i = 0;
        foreach (var layer in sprite.AllLayers)
        {
            if (IsPaintable(ent.Comp, layer))
                _sprite.LayerSetColor((ent, sprite), i, color);

            i++;
        }
    }

    private void OnHeldVisualsUpdated(EntityUid uid, ADTClothingPaintComponent component, HeldVisualsUpdatedEvent args)
    {
        if (component.PaintColor is not { } color)
            return;

        if (!TryComp(args.User, out SpriteComponent? sprite))
            return;

        foreach (var revealed in args.RevealedLayers)
        {
            if (!sprite.LayerMapTryGet(revealed, out var layer) || !IsPaintable(component, sprite[layer]))
                continue;

            _sprite.LayerSetColor((args.User, sprite), layer, color);
        }
    }

    private void OnEquipmentVisualsUpdated(EntityUid uid, ADTClothingPaintComponent component, EquipmentVisualsUpdatedEvent args)
    {
        if (component.PaintColor is not { } color)
            return;

        if (!TryComp(args.Equipee, out SpriteComponent? sprite))
            return;

        foreach (var revealed in args.RevealedLayers)
        {
            if (DisplacementMapHelper.IsDisplacementKey(revealed) ||
                !sprite.LayerMapTryGet(revealed, out var layer) || !IsPaintable(component, sprite[layer]))
                continue;

            _sprite.LayerSetColor((args.Equipee, sprite), layer, color);
        }
    }

    public static bool IsPaintable(ADTClothingPaintComponent? component, ISpriteLayer layer)
    {
        var prefix = component?.TrinketLayerPrefix ?? "trinkets";
        return layer.RsiState.IsValid && layer.RsiState.Name?.StartsWith(prefix) != true;
    }
}
