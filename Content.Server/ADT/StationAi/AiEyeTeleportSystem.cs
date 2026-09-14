using Content.Server.Medical.CrewMonitoring;
using Content.Shared.ADT.StationAi;
using Content.Shared.Chat;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Speech;
using Content.Shared.StationAi;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.ADT.StationAi;

public sealed class AiEyeTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    [Dependency] private readonly StationAiVisionSystem _vision = default!;
    [Dependency] private readonly SharedSuitSensorSystem _suitSensors = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private EntityQuery<BroadphaseComponent> _broadphaseQuery = default!;
    private EntityQuery<MapGridComponent> _gridQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        _broadphaseQuery = GetEntityQuery<BroadphaseComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _net.RegisterNetMessage<MsgAiEyeTeleport>(OnAiEyeTeleport);

        Subs.BuiEvents<CrewMonitoringConsoleComponent>(CrewMonitoringUIKey.Key, subs =>
        {
            subs.Event<CrewMonitoringAiEyeTeleportMessage>(OnCrewMonitorAiEyeTeleport);
        });
    }

    private void OnCrewMonitorAiEyeTeleport(Entity<CrewMonitoringConsoleComponent> ent, ref CrewMonitoringAiEyeTeleportMessage msg)
    {
        if (TryComp<ActorComponent>(msg.Actor, out _))
            TryTeleportAndPopup(msg.Actor, msg.Target, requireCamera: false);
    }

    private void OnAiEyeTeleport(MsgAiEyeTeleport msg)
    {
        var session = _player.GetSessionByChannel(msg.MsgChannel);
        if (session.AttachedEntity is not { } playerEntity)
            return;

        TryTeleportAndPopup(playerEntity, msg.Target, requireCamera: true);
    }

    private void TryTeleportAndPopup(EntityUid aiUid, NetEntity target, bool requireCamera)
    {
        var targetEntity = GetEntity(target);
        if (!targetEntity.IsValid())
            return;

        if (TryTeleportEye(aiUid, targetEntity, requireCamera, out var failReason) || failReason == null)
            return;

        _popup.PopupEntity(failReason, aiUid, aiUid);
    }

    private bool TryTeleportEye(EntityUid aiUid, EntityUid target, bool requireCamera, out string? failReason)
    {
        failReason = null;

        if (!TryGetEye(aiUid, out var core))
            return false;

        if (requireCamera)
        {
            if (!IsValidTarget(target, out failReason))
                return false;
        }
        else if (!HasCoordinatesSensors(target))
        {
            failReason = Loc.GetString("ai-eye-teleport-no-sensors");
            return false;
        }

        TeleportEye(aiUid, core, target);
        return true;
    }

    private bool TryGetEye(EntityUid aiUid, out Entity<StationAiCoreComponent?> core)
    {
        core = default;
        return HasComp<StationAiHeldComponent>(aiUid) &&
               _stationAi.TryGetCore(aiUid, out core) &&
               core.Comp?.RemoteEntity != null;
    }

    private void TeleportEye(EntityUid aiUid, Entity<StationAiCoreComponent?> core, EntityUid target)
    {
        _xforms.SetCoordinates(core.Comp!.RemoteEntity!.Value, Transform(target).Coordinates);
        _popup.PopupEntity(Loc.GetString("ai-eye-teleport-success", ("name", Name(target))), aiUid, aiUid);
    }

    public MsgChatMessage? TryAddRadioEyeLink(EntityUid receiver, MsgChatMessage chatMsg, EntityUid messageSource)
    {
        if (!HasComp<StationAiHeldComponent>(receiver))
            return null;

        var nameEv = new TransformSpeakerNameEvent(messageSource, Name(messageSource));
        RaiseLocalEvent(messageSource, nameEv);

        var voiceName = nameEv.VoiceName;
        if (string.IsNullOrEmpty(voiceName))
            return null;

        var link = $"[aieyelink=\"{FormattedMessage.EscapeStringParameter(voiceName)}\" entity=\"{GetNetEntity(messageSource)}\" title=\"{FormattedMessage.EscapeStringParameter(Loc.GetString("ai-eye-teleport-hover"))}\"/]";
        var escapedName = FormattedMessage.EscapeText(voiceName);

        var headerName = $"{escapedName}[/bold]";
        var message = new ChatMessage(chatMsg.Message.Channel, chatMsg.Message.Message, chatMsg.Message.WrappedMessage, chatMsg.Message.SenderEntity, chatMsg.Message.SenderKey, chatMsg.Message.HideChat, chatMsg.Message.MessageColorOverride, chatMsg.Message.AudioPath, chatMsg.Message.AudioVolume)
        {
            WrappedMessage = chatMsg.Message.WrappedMessage.Replace(headerName, $"{link}[/bold]"),
        };

        return new MsgChatMessage { Message = message };
    }

    private bool IsValidTarget(EntityUid target, out string? failReason)
    {
        failReason = null;

        if (HasComp<StationAiVisionComponent>(target))
            return true;

        if (!HasCoordinatesSensors(target))
        {
            failReason = Loc.GetString("ai-eye-teleport-no-sensors");
            return false;
        }

        if (!IsOnCamera(target))
        {
            failReason = Loc.GetString("ai-eye-teleport-not-on-camera");
            return false;
        }

        return true;
    }

    private bool HasCoordinatesSensors(EntityUid target)
    {
        var targetNetEnt = GetNetEntity(target);
        var query = EntityQueryEnumerator<SuitSensorComponent, TransformComponent>();

        while (query.MoveNext(out var sensorUid, out var sensor, out var sensorXform))
        {
            var status = _suitSensors.GetSensorState((sensorUid, sensor, sensorXform));
            if (status == null ||
                status.Mode != SuitSensorMode.SensorCords ||
                status.Coordinates == null)
            {
                continue;
            }

            if (status.OwnerUid == targetNetEnt)
                return true;
        }

        return false;
    }

    private bool IsOnCamera(EntityUid target)
    {
        var xform = Transform(target);
        if (xform.GridUid == null)
            return false;

        if (!_gridQuery.TryComp(xform.GridUid.Value, out var grid) ||
            !_broadphaseQuery.TryComp(xform.GridUid.Value, out var broadphase))
        {
            return false;
        }

        var tile = _maps.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);

        lock (_vision)
        {
            return _vision.IsAccessible((xform.GridUid.Value, broadphase, grid), tile);
        }
    }
}
