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
    private static ProtoId<DepartmentPrototype> _department = "Security";
    private static ProtoId<AccessLevelPrototype> _extended = "ADTExtended";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AlertLevelChangedEvent>(AccessUdate);
    }
    private void AccessUdate(AlertLevelChangedEvent args)
    {
        ///
        /// во все коды кроме зелёного, синего и фиолетового вылаёт сбшникам дополнительные доступы
        ///
        if (args.AlertLevel != "green" && args.AlertLevel != "blue" && args.AlertLevel != "purple")
        {
            var query = EntityQueryEnumerator<IdCardComponent>();
            while (query.MoveNext(out var uid, out var card))
            {
                if (card.JobDepartments.Contains(_department) && TryComp<AccessComponent>(uid, out var id))
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
                if (card.JobDepartments.Contains(_department) && TryComp<AccessComponent>(uid, out var id))
                {
                    RemoveAccess(uid, id, _extended);
                }
            }
        }
    }
}
