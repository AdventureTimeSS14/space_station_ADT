using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed class NoClipSystem : EntitySystem
{
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoClipComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<NoClipComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<NoClipComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NoClipComponent, ComponentRemove>(OnRemove);
    }

    private void OnStartup(Entity<NoClipComponent> ent, ref ComponentStartup args)
    {
        RegenerateContacts(ent.Owner);
    }

    private void OnRemove(Entity<NoClipComponent> ent, ref ComponentRemove args)
    {
        RegenerateContacts(ent.Owner);
    }

    private void RegenerateContacts(EntityUid uid)
    {
        if (!TryComp(uid, out PhysicsComponent? physics) ||
            !TryComp(uid, out FixturesComponent? fixtures) ||
            !TryComp(uid, out TransformComponent? xform))
            return;

        _broadphase.RegenerateContacts((uid, physics, fixtures, xform));
    }

    private void OnExamine(Entity<NoClipComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineLoc is { } loc)
            args.PushMarkup(Loc.GetString(loc, ("target", Identity.Entity(ent, EntityManager))));
    }

    private void OnPreventCollide(Entity<NoClipComponent> ent, ref PreventCollideEvent args)
    {
        if (!args.OtherFixture.Hard)
            return;

        args.Cancelled = true;
    }
}
