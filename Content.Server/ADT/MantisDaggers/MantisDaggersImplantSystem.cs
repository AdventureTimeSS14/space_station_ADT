using Content.Server.ADT.Implants;
using Content.Shared.ADT.Implants;
using Content.Shared.Body;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.ADT.MantisDaggers;

namespace Content.Server.ADT.MantisDaggers;

public sealed class MantisDaggersImplantSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private VisibleImplantSystem _visibleImplant = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MantisDaggersImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<MantisDaggersImplantComponent, ImplantRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<ImplantedComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<ImplantedComponent, OrganGotRemovedEvent>(OnOrganRemoved);
    }

    private void OnImplanted(EntityUid uid, MantisDaggersImplantComponent comp, ref ImplantImplantedEvent args)
    {
        RefreshHostBody(args.Implanted);
    }

    private void OnRemoved(EntityUid uid, MantisDaggersImplantComponent comp, ref ImplantRemovedEvent args)
    {
        RefreshHostBody(args.Implanted);
    }

    private void OnOrganInserted(Entity<ImplantedComponent> ent, ref OrganGotInsertedEvent args)
    {
        RefreshBody(args.Target);
    }

    private void OnOrganRemoved(Entity<ImplantedComponent> ent, ref OrganGotRemovedEvent args)
    {
        RefreshBody(args.Target);
    }

    private void RefreshHostBody(EntityUid host)
    {
        if (CompOrNull<OrganComponent>(host)?.Body is { } body)
            RefreshBody(body);
    }

    private void RefreshBody(EntityUid body)
    {
        var sides = MantisDaggerSide.None;
        foreach (var organ in _body.EnumerateOrgans<ImplantedComponent>(body))
        {
            foreach (var implant in organ.Comp2.ImplantContainer.ContainedEntities)
            {
                if (TryComp<MantisDaggersImplantComponent>(implant, out var dagger))
                    sides |= dagger.Side;
            }
        }

        _visibleImplant.SetDaggerSides(body, sides);
    }
}



