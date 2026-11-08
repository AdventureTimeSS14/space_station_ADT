using Content.Server.ADT.Salvage.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared.ADT.Legion;
using Content.Shared.ADT.Salvage;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Legion;

public sealed class ADTLegionGateSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLegionGateComponent, InteractHandEvent>(OnInteractHand);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTLegionGateComponent>();

        while (query.MoveNext(out var uid, out var gate))
        {
            if (gate.ReleaseAt is not { } releaseAt || now < releaseAt)
                continue;

            gate.ReleaseAt = null;
            Release((uid, gate), gate.Knocker, false);
            gate.Knocker = null;
        }
    }

    private void OnInteractHand(Entity<ADTLegionGateComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;

        if (ent.Comp.Locked || ent.Comp.Triggered)
        {
            args.Handled = true;

            var message = IsOpen(ent) ? "adt-legion-gate-forever-open" : "adt-legion-gate-sealed";
            _popup.PopupEntity(Loc.GetString(message), ent, user);
            return;
        }

        if (!ent.Comp.LegionGate)
            return;

        args.Handled = true;

        if (ent.Comp.ReleaseAt != null || IsOpen(ent))
            return;

        var now = _timing.CurTime;

        if (!ent.Comp.Confirmed.TryGetValue(user, out var confirmedUntil) || now > confirmedUntil)
        {
            ent.Comp.Confirmed[user] = now + ent.Comp.ConfirmWindow;
            _popup.PopupEntity(Loc.GetString("adt-legion-gate-warning"), ent, user, PopupType.MediumCaution);
            return;
        }

        ent.Comp.Confirmed.Remove(user);

        _popup.PopupEntity(
            Loc.GetString("adt-legion-gate-knock-others", ("user", user), ("gate", ent.Owner)),
            ent,
            Filter.PvsExcept(user),
            true,
            PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("adt-legion-gate-knock-self", ("gate", ent.Owner)), ent, user, PopupType.MediumCaution);
        _audio.PlayPvs(ent.Comp.KnockSound, ent);

        ent.Comp.Knocker = user;
        ent.Comp.ReleaseAt = now + ent.Comp.KnockDelay;
    }

    public void TryReleaseByLegion(EntityUid legion)
    {
        var mapId = _transform.GetMapId(legion);

        if (mapId == MapId.Nullspace)
            return;

        var query = EntityQueryEnumerator<ADTLegionGateComponent>();

        while (query.MoveNext(out var uid, out var gate))
        {
            if (!gate.LegionGate || gate.Triggered)
                continue;

            if (_transform.GetMapId(uid) != mapId)
                continue;

            Release((uid, gate), null, true);
        }
    }

    private void Release(Entity<ADTLegionGateComponent> ent, EntityUid? user, bool legionDamaged)
    {
        if (ent.Comp.Triggered)
            return;

        ent.Comp.Triggered = true;
        ent.Comp.Locked = true;

        if (TryComp<DoorComponent>(ent, out var door) && door.State != DoorState.Open)
            _door.StartOpening(ent, door);

        _popup.PopupEntity(Loc.GetString("adt-legion-gate-released"), ent, PopupType.LargeCaution);

        if (legionDamaged)
            _adminLog.Add(LogType.Action, LogImpact.High, $"Legion took damage while the necropolis gate {ToPrettyString(ent):gate} was closed and released itself");
        else if (user != null)
            _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(user.Value):user} released Legion through {ToPrettyString(ent):gate}");
        else
            _adminLog.Add(LogType.Action, LogImpact.High, $"Legion was released through {ToPrettyString(ent):gate}");

        var mapId = _transform.GetMapId(ent.Owner);

        if (mapId == MapId.Nullspace)
            return;

        var whispers = Loc.GetString("adt-legion-gate-whispers");
        var released = Loc.GetString("adt-legion-gate-released-global");
        var filter = Filter.BroadcastMap(mapId);

        _chat.ChatMessageToManyFiltered(filter, ChatChannel.Server, whispers, $"[color=red][bold]{FormattedMessage.EscapeText(whispers)}[/bold][/color]", default, false, true, null);
        _chat.ChatMessageToManyFiltered(filter, ChatChannel.Server, released, $"[color=red][bold]{FormattedMessage.EscapeText(released)}[/bold][/color]", default, false, true, null);

        _audio.PlayGlobal(ent.Comp.ReleaseSound, filter, true);
        RaiseNetworkEvent(new ADTMegafaunaScreenFlashEvent(ent.Comp.ReleaseFlashColor, (float) ent.Comp.ReleaseFlashDuration.TotalSeconds), filter);

        if (user is { } knocker)
            SendLegionAfter(mapId, knocker);
    }

    private void SendLegionAfter(MapId mapId, EntityUid knocker)
    {
        var query = EntityQueryEnumerator<ADTLegionMegafaunaComponent, MegafaunaComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var megafauna, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            _megafauna.TryAggro((uid, megafauna), knocker);
        }
    }

    private bool IsOpen(EntityUid uid)
    {
        return TryComp<DoorComponent>(uid, out var door) && door.State is DoorState.Open or DoorState.Opening;
    }
}
