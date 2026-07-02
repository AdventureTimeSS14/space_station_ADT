using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.ADT.MindSlave.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.ADT.MindSlave;

/// <summary>
/// Manages the MindSlaveComponent lifecycle — startup, shutdown, and mindshield-based freedom.
/// </summary>
public sealed class MindSlaveSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly ISharedChatManager _chat = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    /// <summary>
    /// Subscribes to <see cref="ComponentStartup"/>, <see cref="ComponentShutdown"/>, and <see cref="EntGotInsertedIntoContainerMessage"/> for mindshield detection.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MindSlaveComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EntGotInsertedIntoContainerMessage>(OnImplantInserted, before: new[] { typeof(SharedSubdermalImplantSystem) });
    }

    private void OnStartup(Entity<MindSlaveComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<ImplantedComponent>(ent, out var implantedComp))
        {
            foreach (var implantEntity in implantedComp.ImplantContainer.ContainedEntities)
            {
                if (HasComp<MindShieldImplantComponent>(implantEntity))
                {
                    _container.Remove(implantEntity, implantedComp.ImplantContainer);
                    QueueDel(implantEntity);
                    break;
                }
            }
        }
    }

    private void OnShutdown(Entity<MindSlaveComponent> ent, ref ComponentShutdown args)
    {
        if (_mindSystem.TryGetMind(ent, out var mindId, out var mind))
        {
            _roleSystem.MindRemoveRole<MindSlaveRoleComponent>(mindId);
        }
    }

    private void OnImplantInserted(EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ImplanterComponent.ImplantSlotId)
            return;

        var target = args.Container.Owner;

        if (!HasComp<MindSlaveComponent>(target))
            return;

        if (!HasComp<MindShieldImplantComponent>(args.Entity))
            return;

        Entity<MindSlaveComponent> ent = (target, Comp<MindSlaveComponent>(target));
        var mindSlave = ent.Comp;
        var name = Identity.Entity(ent, EntityManager);
        var stunTime = TimeSpan.FromSeconds(10);

        RemComp<MindSlaveComponent>(ent);

        if (_mindSystem.TryGetMind(ent, out var mindId, out var mind))
        {
            _roleSystem.MindRemoveRole<MindSlaveRoleComponent>(mindId);
        }

        var master = mindSlave.Master;
        if (master.IsValid() && Exists(master))
        {
            var hasOtherSlaves = false;
            var query = EntityQueryEnumerator<MindSlaveComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (uid != ent.Owner && comp.Master == master)
                {
                    hasOtherSlaves = true;
                    break;
                }
            }
            if (!hasOtherSlaves)
                RemComp<MindSlaveMasterComponent>(master);
        }

        _stun.TryUpdateParalyzeDuration(ent, stunTime);
        _popup.PopupEntity(Loc.GetString("mindslave-break-control", ("name", name)), ent, PopupType.LargeCaution);

        var filter = Filter.Pvs(ent, entityManager: EntityManager);
        _audio.PlayGlobal("/Audio/ADT/MindSlave/alarm4.ogg", filter, true);

        _adminLog.Add(LogType.Mind, LogImpact.Extreme,
            $"{ToPrettyString(ent)} was freed from mindslave control by a mindshield");
        _chat.SendAdminAlert(Loc.GetString("mindslave-break-control-log", ("name", ToPrettyString(ent))));
    }
}
