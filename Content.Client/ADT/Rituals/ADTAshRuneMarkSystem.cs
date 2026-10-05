using Content.Shared.ADT.Rituals;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Rituals;

public sealed class ADTAshRuneMarkSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAshRuneMarkComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ADTAshRuneMarkComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnStartup(Entity<ADTAshRuneMarkComponent> ent, ref ComponentStartup args)
    {
        UpdateVisible(ent);
    }

    private void OnState(Entity<ADTAshRuneMarkComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisible(ent);
    }

    private void UpdateVisible(Entity<ADTAshRuneMarkComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        _sprite.SetVisible((ent.Owner, sprite), ent.Comp.Lit);
    }
}
