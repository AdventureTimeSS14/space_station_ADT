using Content.Shared.ADT.Sponsors.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Sponsors;

public sealed class SponsorGhostColorSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SponsorGhostColorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SponsorGhostColorComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<SponsorGhostColorComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<SponsorGhostColorComponent> ent, ref ComponentStartup args)
    {
        Apply(ent, ent.Comp.Color);
    }

    private void OnState(Entity<SponsorGhostColorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Apply(ent, ent.Comp.Color);
    }

    private void OnShutdown(Entity<SponsorGhostColorComponent> ent, ref ComponentShutdown args)
    {
        Apply(ent, Color.White);
    }

    private void Apply(EntityUid uid, Color color)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetColor((uid, sprite), color);
    }
}
