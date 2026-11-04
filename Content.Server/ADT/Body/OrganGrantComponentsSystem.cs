using Content.Shared.ADT.Body.OrganGrant;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Body;

public sealed class OrganGrantComponentsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganGrantComponentsComponent, OrganGotInsertedEvent>(OnInserted);
        SubscribeLocalEvent<OrganGrantComponentsComponent, OrganGotRemovedEvent>(OnRemoved);
    }

    private void OnInserted(Entity<OrganGrantComponentsComponent> ent, ref OrganGotInsertedEvent args)
    {
        var body = args.Target;
        if (TerminatingOrDeleted(body))
            return;

        var tracker = EnsureComp<OrganGrantTrackerComponent>(body);

        foreach (var (name, entry) in ent.Comp.Components)
        {
            if (!tracker.Sources.TryGetValue(name, out var sources))
            {
                sources = new HashSet<EntityUid>();
                tracker.Sources[name] = sources;
            }

            if (sources.Count == 0 && !HasComp(body, entry.Component.GetType()))
            {
                EntityManager.AddComponents(body, new ComponentRegistry { { name, entry } });
                tracker.Owned.Add(name);
            }

            sources.Add(ent.Owner);
        }
    }

    private void OnRemoved(Entity<OrganGrantComponentsComponent> ent, ref OrganGotRemovedEvent args)
    {
        var body = args.Target;
        if (TerminatingOrDeleted(body) || !TryComp<OrganGrantTrackerComponent>(body, out var tracker))
            return;

        foreach (var (name, entry) in ent.Comp.Components)
        {
            if (!tracker.Sources.TryGetValue(name, out var sources))
                continue;

            sources.Remove(ent.Owner);
            if (sources.Count > 0)
                continue;

            tracker.Sources.Remove(name);

            if (tracker.Owned.Remove(name))
                EntityManager.RemoveComponents(body, new ComponentRegistry { { name, entry } });
        }
    }
}
