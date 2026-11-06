using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Clothing.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.ADT.Lavaland;

public sealed class ADTGoliathPlatingSystem : SharedADTGoliathPlatingSystem
{
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private ClothingSystem _clothing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGoliathPlatableComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<ADTGoliathPlatableComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<ADTGoliathPlatableComponent> ent, ref ComponentStartup args)
    {
        UpdateVisuals(ent);
    }

    private void OnHandleState(Entity<ADTGoliathPlatableComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    protected override void UpdateVisuals(Entity<ADTGoliathPlatableComponent> ent)
    {
        if (ent.Comp.Sprites.Count == 0)
            return;

        var path = ent.Comp.Sprites[Math.Clamp(ent.Comp.Plates, 0, ent.Comp.Sprites.Count - 1)];
        if (!_cache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / path, out var rsi))
            return;

        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetBaseRsi((ent.Owner, sprite), rsi.RSI);

        _clothing.SetRsiPath(ent, path);
    }
}
