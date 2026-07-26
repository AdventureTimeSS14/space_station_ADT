namespace Content.Shared.Body;

public sealed partial class BodySystem
{
    public bool TryRemoveOrgan(Entity<OrganComponent?> organ)
    {
        if (!_organQuery.Resolve(organ, ref organ.Comp, false))
            return false;

        if (organ.Comp.Body is not { } bodyUid)
            return false;

        if (!_bodyQuery.TryComp(bodyUid, out var bodyComp) || bodyComp.Organs == null)
            return false;

        return _container.Remove(organ.Owner, bodyComp.Organs);
    }

    public bool TryInsertOrgan(Entity<BodyComponent?> body, EntityUid organ)
    {
        if (!_bodyQuery.Resolve(body, ref body.Comp, false) || body.Comp.Organs == null)
            return false;

        return _container.Insert(organ, body.Comp.Organs);
    }
}
