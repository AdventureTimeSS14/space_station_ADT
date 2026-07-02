using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.ADT.MindSlave.Components;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Popups;

namespace Content.Server.ADT.MindSlave;

/// <summary>
/// Handles removal of the MindSlave implant and cleans up associated components and roles.
/// </summary>
public sealed class MindSlaveImplantSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;

    /// <summary>
    /// Subscribes to <see cref="ImplantRemovedEvent"/> for <see cref="MindSlaveImplantComponent"/>.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveImplantComponent, ImplantRemovedEvent>(OnRemoved);
    }

    private void OnRemoved(Entity<MindSlaveImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        var implanted = args.Implanted;

        if (!TryComp<MindSlaveComponent>(implanted, out var mindSlave))
            return;

        var master = mindSlave.Master;

        RemComp<MindSlaveComponent>(implanted);

        if (_mindSystem.TryGetMind(implanted, out var mindId, out var mind))
        {
            _roleSystem.MindRemoveRole<MindSlaveRoleComponent>(mindId);
        }

        if (master.IsValid() && master != implanted && Exists(master))
        {
            var hasOtherSlaves = false;
            var query = EntityQueryEnumerator<MindSlaveComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (uid != implanted && comp.Master == master)
                {
                    hasOtherSlaves = true;
                    break;
                }
            }
            if (!hasOtherSlaves)
                RemComp<MindSlaveMasterComponent>(master);
        }

        _popup.PopupEntity(Loc.GetString("mindslave-implant-removed"), implanted, implanted, PopupType.Large);

        _adminLog.Add(LogType.Mind, LogImpact.Medium,
            $"{ToPrettyString(implanted)} is no longer a mindslave (implant removed)");
    }
}
