using Content.Server.Access.Systems;
using Content.Server.ADT.BlueShield.Components;
using Content.Server.AlertLevel;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.ADT.AlertAccessLevel;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.AlertAccessLevel;

public sealed class AlertAccessLevel : SharedAlertAccessLevel
{
    private static ProtoId<DepartmentPrototype> _secdepartment = "Security";
    private static ProtoId<DepartmentPrototype> _engdepartment = "Engineering";
    private static ProtoId<AccessLevelPrototype> _extended = "ADTExtended";
    private static List<string> _noSecAccessCode = ["purple", "blue", "green", "yellow"]; //он отвечает за то, когда у сб доступа не будет
    private static List<string> _engieAccessCode = ["yellow"]; //когда у инжей есть доступы
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AlertLevelChangedEvent>(AccessUpdate);
    }
    private void AccessUpdate(AlertLevelChangedEvent args)
    {
        #region sec
        if (!_noSecAccessCode.Contains(args.AlertLevel))
        {
            var query = EntityQueryEnumerator<IdCardComponent>();
            while (query.MoveNext(out var uid, out var card))
            {
                if (card.JobDepartments.Contains(_secdepartment) && TryComp<AccessComponent>(uid, out var id))
                {
                    AddAccess(uid, id, _extended);
                }
            }
        }
        else
        {
            var query = EntityQueryEnumerator<IdCardComponent>();
            while (query.MoveNext(out var uid, out var card))
            {
                if (card.JobDepartments.Contains(_secdepartment) && TryComp<AccessComponent>(uid, out var id))
                {
                    RemoveAccess(uid, id, _extended);
                }
            }
        }
        #endregion

        #region engie
        if (_engieAccessCode.Contains(args.AlertLevel))
        {
            var query = EntityQueryEnumerator<IdCardComponent>();
            while (query.MoveNext(out var uid, out var card))
            {
                if (card.JobDepartments.Contains(_engdepartment) && TryComp<AccessComponent>(uid, out var id))
                {
                    AddAccess(uid, id, _extended);
                }
            }
        }
        else
        {
            var query = EntityQueryEnumerator<IdCardComponent>();
            while (query.MoveNext(out var uid, out var card))
            {
                if (card.JobDepartments.Contains(_engdepartment) && TryComp<AccessComponent>(uid, out var id))
                {
                    RemoveAccess(uid, id, _extended);
                }
            }
        }
        #endregion
    }
}
