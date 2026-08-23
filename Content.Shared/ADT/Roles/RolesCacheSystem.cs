using Content.Shared.ADT.Ghost;
using Content.Shared.Mind;
using Content.Shared.Roles;
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
            component.VisibleAntagName = null;
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

        if (args.RoleComponent.Antag && TryComp<GhostVisibleAntagComponent>(args.RoleUid, out var visible))
            cacheComp.VisibleAntagName = Loc.GetString(visible.Name ?? GetAntagNameLoc(args.RoleComponent.AntagPrototype));
    }

    private LocId GetAntagNameLoc(ProtoId<AntagPrototype>? antagProto)
    {
        if (antagProto is { } protoId && _prototypes.TryIndex(protoId, out var antag))
            return antag.Name;

        return "roles-antag-generic-solo-antagonist-name";
    }
}