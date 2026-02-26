using Content.Shared.Access;
using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.AlertAccessLevel;

public abstract class SharedAlertAccessLevel : EntitySystem
{

    public void AddAccess(EntityUid uid, AccessComponent comp, ProtoId<AccessLevelPrototype> extended)
    {
        comp.Tags.Add(extended);
        Dirty(uid, comp);
    }
    public void RemoveAccess(EntityUid uid, AccessComponent comp, ProtoId<AccessLevelPrototype> extended)
    {
        comp.Tags.Remove(extended);
        Dirty(uid, comp);
    }
}
