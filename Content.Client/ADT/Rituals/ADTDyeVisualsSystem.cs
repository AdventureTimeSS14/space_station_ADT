using Content.Shared.ADT.Rituals;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Rituals;

public sealed class ADTDyeVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDyeVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ADTDyedComponent, ComponentStartup>(OnDyedStartup);
        SubscribeLocalEvent<ADTDyedComponent, AfterAutoHandleStateEvent>(OnDyedState);
    }

    private void OnStartup(Entity<ADTDyeVisualsComponent> ent, ref ComponentStartup args)
    {
        UpdateVisuals(ent.Owner);
    }

    private void OnDyedStartup(Entity<ADTDyedComponent> ent, ref ComponentStartup args)
    {
        UpdateVisuals(ent.Owner);
    }

    private void OnDyedState(Entity<ADTDyedComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent.Owner);
    }

    private void UpdateVisuals(EntityUid uid)
    {
        if (!TryComp<ADTDyeVisualsComponent>(uid, out var visuals) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), visuals.Layer, out var layer, false))
            return;

        var dye = CompOrNull<ADTDyedComponent>(uid)?.Dye;

        if (dye == null)
        {
            _sprite.LayerSetVisible((uid, sprite), layer, false);
            return;
        }

        _sprite.LayerSetRsiState((uid, sprite), layer, $"{visuals.Prefix}_{dye.ToLowerInvariant()}");
        _sprite.LayerSetVisible((uid, sprite), layer, true);
    }
}

