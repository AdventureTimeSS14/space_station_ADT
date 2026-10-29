using Content.Shared.ADT.Surgery.Components;
using Content.Shared.Body;
using Content.Shared.CombatMode.Pacification;

namespace Content.Server.ADT.Surgery;

public sealed class LobotomySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LobotomizedOrganComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LobotomizedOrganComponent, OrganGotInsertedEvent>(OnInserted);
        SubscribeLocalEvent<LobotomizedOrganComponent, OrganGotRemovedEvent>(OnRemoved);
    }

    private void OnStartup(Entity<LobotomizedOrganComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<OrganComponent>(ent, out var organ) && organ.Body is { } body)
            EnsureComp<PacifiedComponent>(body);
    }

    private void OnInserted(Entity<LobotomizedOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        EnsureComp<PacifiedComponent>(args.Target);
    }

    private void OnRemoved(Entity<LobotomizedOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!TerminatingOrDeleted(args.Target))
            RemComp<PacifiedComponent>(args.Target);
    }
}
