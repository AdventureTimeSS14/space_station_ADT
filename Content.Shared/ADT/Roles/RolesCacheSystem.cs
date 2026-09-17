using Content.Shared.ADT.Ghost;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Roles;

public sealed class RolesCacheSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MindComponent, RoleAddingEvent>(OnRoleAdded);
        SubscribeLocalEvent<MindComponent, RoleRemovingEvent>(OnRoleRemoved);
    }

    private void OnRoleRemoved(EntityUid uid, MindComponent mind, RoleRemovingEvent args)
    {
        if (!TryComp<RoleCacheComponent>(mind.CurrentEntity, out var component))
            return;

        if (args.RoleComponent.Antag)
            component.AntagWeight -= 1;

        if (TryComp<GhostVisibleAntagComponent>(args.RoleUid, out _))
            component.VisibleAntagName = RecalculateVisibleAntagName(mind, args.RoleUid);
    }

    private void OnRoleAdded(EntityUid uid, MindComponent component, RoleAddingEvent args)
    {
        if (component.CurrentEntity is null)
            return;

        var cacheComp = EnsureComp<RoleCacheComponent>(component.CurrentEntity.Value);

        if (args.RoleComponent.Antag)
            cacheComp.AntagWeight += 1;

        if (args.RoleComponent.JobPrototype != null)
            cacheComp.LastJobPrototype = args.RoleComponent.JobPrototype;
        if (args.RoleComponent.AntagPrototype != null)
            cacheComp.LastAntagPrototype = args.RoleComponent.AntagPrototype;

        cacheComp.VisibleAntagName = RecalculateVisibleAntagName(component, default);
    }

    private string? RecalculateVisibleAntagName(MindComponent mind, EntityUid skipRole)
    {
        string? name = null;

        foreach (var role in mind.MindRoleContainer.ContainedEntities)
        {
            if (role == skipRole)
                continue;

            if (!TryComp<MindRoleComponent>(role, out var roleComp) || !roleComp.Antag)
                continue;

            if (!TryComp<GhostVisibleAntagComponent>(role, out var visible))
                continue;

            name = Loc.GetString(visible.Name ?? GetAntagNameLoc(roleComp.AntagPrototype));
        }

        return name;
    }

    private LocId GetAntagNameLoc(ProtoId<AntagPrototype>? antagProto)
    {
        if (antagProto is { } protoId && _prototypes.TryIndex(protoId, out var antag))
            return antag.Name;

        return "roles-antag-generic-solo-antagonist-name";
    }
}