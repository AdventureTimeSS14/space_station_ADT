using Content.Shared.ADT.StationRadio.Components;
using Content.Shared.ADT.StationRadio.Events;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.ADT.StationRadio.Systems;

public sealed class VinylPlayerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLinkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VinylPlayerComponent, EntInsertedIntoContainerMessage>(OnVinylInserted);
        SubscribeLocalEvent<VinylPlayerComponent, EntRemovedFromContainerMessage>(OnVinylRemove);
        SubscribeLocalEvent<VinylPlayerComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<VinylPlayerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(EntityUid uid, VinylPlayerComponent comp, PowerChangedEvent args)
    {
        if (comp.SoundEntity != null && !args.Powered)
            comp.SoundEntity = _audio.Stop(comp.SoundEntity);

        if (!comp.RelayToRadios || !CheckForRadioServer(uid, out var radioServer) || !TryComp<StationRadioServerComponent>(radioServer, out var serverComp) || serverComp.ChannelId == null)
            return;

        var channelId = serverComp.ChannelId;

        var ev = new StationRadioMediaStoppedEvent(channelId);
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out var receiverComp))
        {
            if (receiverComp.SelectedChannelId == channelId)
                RaiseLocalEvent(receiver, ev);
        }
    }

    private void OnDestruction(EntityUid uid, VinylPlayerComponent comp, DestructionEventArgs args)
    {
        if (!comp.RelayToRadios || !CheckForRadioServer(uid, out var radioServer) || !TryComp<StationRadioServerComponent>(radioServer, out var serverComp) || serverComp.ChannelId == null)
            return;

        var channelId = serverComp.ChannelId;

        var ev = new StationRadioMediaStoppedEvent(channelId);
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out var receiverComp))
        {
            if (receiverComp.SelectedChannelId == channelId)
                RaiseLocalEvent(receiver, ev);
        }
    }

    private void OnVinylInserted(EntityUid uid, VinylPlayerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp(args.Entity, out VinylComponent? vinylcomp) || _net.IsClient || vinylcomp.Song == null || !_power.IsPowered(uid))
            return;

        var audio = _audio.PlayPredicted(vinylcomp.Song, uid, uid, AudioParams.Default.WithVolume(3f).WithMaxDistance(4.5f));
        if (audio != null)
            comp.SoundEntity = audio.Value.Entity;

        comp.InsertedVinyl = args.Entity;

        if (!comp.RelayToRadios || !CheckForRadioServer(uid, out var radioServer) || !TryComp<StationRadioServerComponent>(radioServer, out var serverComp) || serverComp.ChannelId == null)
            return;

        var channelId = serverComp.ChannelId;

        var ev = new StationRadioMediaPlayedEvent(vinylcomp.Song, channelId);
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out var receiverComp))
        {
            if (receiverComp.SelectedChannelId == channelId && !receiverComp.SoundEntity.HasValue)
                RaiseLocalEvent(receiver, ev);
        }
    }

    private void OnVinylRemove(EntityUid uid, VinylPlayerComponent comp, EntRemovedFromContainerMessage args)
    {
        if (comp.SoundEntity != null)
            comp.SoundEntity = _audio.Stop(comp.SoundEntity);

        comp.InsertedVinyl = null;

        if (!comp.RelayToRadios || !CheckForRadioServer(uid, out var radioServer) || !TryComp<StationRadioServerComponent>(radioServer, out var serverComp) || serverComp.ChannelId == null)
            return;

        var channelId = serverComp.ChannelId;

        var ev = new StationRadioMediaStoppedEvent(channelId);
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out var receiverComp))
        {
            if (receiverComp.SelectedChannelId == channelId)
                RaiseLocalEvent(receiver, ev);
        }
    }

    private bool CheckForRadioServer(EntityUid uid, out EntityUid radioServer)
    {
        radioServer = EntityUid.Invalid;

        if (!TryComp<DeviceLinkSourceComponent>(uid, out var vinylSource))
            return false;

        foreach (var rig in vinylSource.LinkedPorts.Keys)
        {
            if (!HasComp<RadioRigComponent>(rig))
                continue;

            if (!TryComp<DeviceLinkSourceComponent>(rig, out var rigSource))
                continue;

            foreach (var server in rigSource.LinkedPorts.Keys)
            {
                if (HasComp<StationRadioServerComponent>(server))
                {
                    radioServer = server;
                    return true;
                }
            }
        }
        return false;
    }
}