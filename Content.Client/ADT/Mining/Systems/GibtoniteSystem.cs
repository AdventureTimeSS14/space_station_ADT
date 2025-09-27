using Content.Shared.ADT.Mining.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Mining.Systems;

public sealed class GibtoniteSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GibtoniteComponent, AppearanceChangeEvent>(ChangeAppearanceSprite);
        SubscribeLocalEvent<GibtoniteComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, GibtoniteComponent comp, ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.LayerSetVisible(0, true); // Спрайт гибтонита
            sprite.LayerSetVisible(1, false); // Спрайт активности
            sprite.LayerSetVisible(2, false); // Спрайт нормы
            sprite.LayerSetVisible(3, false); // OHFUCK
        }
    }

    public void ChangeAppearanceSprite(EntityUid uid, GibtoniteComponent comp, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_appearance.TryGetData<bool>(uid, GibtonitState.State, out var Active))
        {
            if (comp.Active)
            {
                sprite.LayerSetVisible(1, true);
            }
        
            switch (comp.State)
            {
                case GibtoniteState.OhFuck:
                    sprite.LayerSetVisible(3, true);
                    break;

                case GibtoniteState.Normal:
                    sprite.LayerSetVisible(2, true);
                    break;

                default:
                    sprite.LayerSetVisible(3, false);
                    break;
            }
        }
    }
}