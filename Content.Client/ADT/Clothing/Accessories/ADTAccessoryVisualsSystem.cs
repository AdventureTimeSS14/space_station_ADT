using Content.Client.Clothing;
using Content.Shared.ADT.Clothing.Accessories;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Clothing.Accessories;

public sealed class ADTAccessoryVisualsSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _cache = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAccessoryHolderComponent, GetEquipmentVisualsEvent>(OnGetVisuals, after: new[] { typeof(ClientClothingSystem) });
    }

    private void OnGetVisuals(Entity<ADTAccessoryHolderComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        var i = 0;
        foreach (var accessory in ent.Comp.Container.ContainedEntities)
        {
            if (!TryComp<ADTAccessoryComponent>(accessory, out var comp))
                continue;

            if (GetRsiPath(accessory, comp) is not { } path)
                continue;

            if (!_cache.TryGetResource<RSIResource>(path, out var rsi) || !rsi.RSI.TryGetState(comp.EquippedState, out _))
                continue;

            var layer = new PrototypeLayerData
            {
                RsiPath = path.ToString(),
                State = comp.EquippedState,
            };

            args.Layers.Add(($"{args.Slot}-adt-accessory-{i}", layer));
            i++;
        }
    }

    private ResPath? GetRsiPath(EntityUid accessory, ADTAccessoryComponent comp)
    {
        if (comp.Sprite is { } sprite)
            return SpriteSpecifierSerializer.TextureRoot / sprite;

        if (TryComp<ClothingComponent>(accessory, out var clothing) && clothing.RsiPath != null)
            return SpriteSpecifierSerializer.TextureRoot / clothing.RsiPath;

        if (TryComp<SpriteComponent>(accessory, out var spriteComp) && spriteComp.BaseRSI != null)
            return spriteComp.BaseRSI.Path;

        return null;
    }
}
