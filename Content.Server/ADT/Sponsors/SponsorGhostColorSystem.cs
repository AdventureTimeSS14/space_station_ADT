using Content.Shared.ADT.Sponsors.Components;
using Content.Shared.Ghost;
using Robust.Shared.Player;

namespace Content.Server.ADT.Sponsors;

public sealed class SponsorGhostColorSystem : EntitySystem
{
    [Dependency] private readonly SponsorManager _sponsors = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostAttached);
    }

    private void OnGhostAttached(Entity<GhostComponent> ent, ref PlayerAttachedEvent args)
    {
        var color = _sponsors.GetGhostColor(args.Player.UserId);

        if (color == null)
        {
            RemComp<SponsorGhostColorComponent>(ent);
            return;
        }

        var comp = EnsureComp<SponsorGhostColorComponent>(ent);
        comp.Color = color.Value;
        Dirty(ent, comp);
    }
}
