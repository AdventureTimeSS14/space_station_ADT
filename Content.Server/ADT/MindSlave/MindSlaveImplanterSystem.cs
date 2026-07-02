using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.ADT.MindSlave;
using Content.Shared.ADT.MindSlave.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.ADT.MindSlave;

/// <summary>
/// Handles the MindSlave implanter item — interaction, do-after, implanting, and visual state.
/// </summary>
public sealed class MindSlaveImplanterSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _subdermalImplant = default!;

    /// <summary>
    /// Subscribes to <see cref="AfterInteractEvent"/> and <see cref="MindSlaveImplantDoAfterEvent"/>.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveImplanterComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MindSlaveImplanterComponent, MindSlaveImplantDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, MindSlaveImplanterComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || args.Handled)
            return;

        var target = args.Target.Value;
        var user = args.User;

        if (target == user)
        {
            _popup.PopupEntity(Loc.GetString("mindslave-self-use-fail"), target, user, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        if (HasComp<MindSlaveComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("mindslave-already-enslaved"), target, user, PopupType.MediumCaution);
            args.Handled = true;
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("mindslave-mindshield-block"), target, user, PopupType.MediumCaution);
            args.Handled = true;
            return;
        }

        if (_mobState.IsDead(target))
        {
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, user, 5f, new MindSlaveImplantDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        _popup.PopupClient(Loc.GetString("injector-component-needle-injecting-user"), target, user);
        var userName = Identity.Entity(user, EntityManager);
        _popup.PopupEntity(Loc.GetString("implanter-component-implanting-target", ("user", userName)), user, target, PopupType.LargeCaution);

        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, MindSlaveImplanterComponent component, MindSlaveImplantDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        var target = args.Target.Value;
        var user = args.User;

        if (HasComp<MindSlaveComponent>(target) || HasComp<MindShieldComponent>(target) || _mobState.IsDead(target))
            return;

        var implant = Spawn("MindSlaveImplant", Transform(target).Coordinates);

        if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
        {
            Log.Warning(Loc.GetString("mindslave-implant-spawn-fail"));
            Del(implant);
            return;
        }

        _subdermalImplant.ForceImplant(target, (implant, implantComp));

        var masterName = Identity.Name(user, EntityManager);

        var mindSlave = AddComp<MindSlaveComponent>(target);
        mindSlave.Master = user;
        mindSlave.MasterName = masterName;
        Dirty(target, mindSlave);

        EnsureComp<MindSlaveMasterComponent>(user);

        if (TryComp<ImplantedComponent>(target, out var implantedComp))
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
        RemComp<MindShieldComponent>(target);

        var filter = Filter.Pvs(target, entityManager: EntityManager);
        _audio.PlayGlobal("/Audio/ADT/MindSlave/alarm4.ogg", filter, true);

        _popup.PopupEntity(Loc.GetString("mindslave-implant-success", ("master", masterName)), target, target, PopupType.Large);

        _appearance.SetData(uid, ImplanterVisuals.Full, false);
        _appearance.SetData(uid, ImplanterImplantOnlyVisuals.ImplantOnly, true);
        RemComp<MindSlaveImplanterComponent>(uid);

        if (_mindSystem.TryGetMind(target, out var mindId, out var mind))
        {
            _roleSystem.MindAddRole(mindId, "MindSlaveRole", mind);

            if (mind.UserId != null && _player.TryGetSessionById(mind.UserId.Value, out var session))
            {
                var message = Loc.GetString("mindslave-implant-success-chat", ("master", masterName));
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
                _chat.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, session.Channel);
            }

            mind.AddMemory(new Memory("MindSlave", Loc.GetString("mindslave-memory", ("master", masterName))));
        }

        _adminLog.Add(LogType.Mind, LogImpact.Extreme,
            $"{ToPrettyString(target)} was mindslaved by {ToPrettyString(user)}");
        _chat.SendAdminAlert(Loc.GetString("mindslave-implant-success-log", ("target", ToPrettyString(target)), ("user", ToPrettyString(user))));

        args.Handled = true;
    }
}
