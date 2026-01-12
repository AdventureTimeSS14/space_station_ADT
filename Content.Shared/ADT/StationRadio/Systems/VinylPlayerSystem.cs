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
        // При потере питания - останавливаем ВСЁ
        if (!args.Powered)
        {
            StopAllSoundsForVinylPlayer(uid, comp);

            if (CheckForRadioServer(uid, out var radioServer) &&
                TryComp<StationRadioServerComponent>(radioServer, out var serverComp))
            {
                StopBroadcast(radioServer, serverComp);
            }
        }
        // При восстановлении питания - запускаем пластинку если она есть
        else if (args.Powered && comp.InsertedVinyl != null &&
                TryComp<VinylComponent>(comp.InsertedVinyl, out var vinylComp) &&
                vinylComp.Song != null)
        {
            StopAllSoundsForVinylPlayer(uid, comp);

            var audioParams = AudioParams.Default.WithVolume(3f).WithMaxDistance(4.5f).WithLoop(true);
            var audio = _audio.PlayPredicted(vinylComp.Song, uid, uid, audioParams);
            if (audio != null)
                comp.SoundEntity = audio.Value.Entity;

            if (CheckForRadioServer(uid, out var radioServer) &&
                TryComp<StationRadioServerComponent>(radioServer, out var serverComp) &&
                serverComp.ChannelId != null)
            {
                StartBroadcast(radioServer, serverComp, vinylComp.Song, serverComp.ChannelId);
            }
        }
    }

    private void StopAllSoundsForVinylPlayer(EntityUid uid, VinylPlayerComponent comp)
    {
        // Останавливаем только сохраненный звук пластинки
        if (comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value))
        {
            _audio.Stop(comp.SoundEntity.Value);
            comp.SoundEntity = null;
        }
    }

    private void OnDestruction(EntityUid uid, VinylPlayerComponent comp, DestructionEventArgs args)
    {
        StopAllSoundsForVinylPlayer(uid, comp);

        if (CheckForRadioServer(uid, out var radioServer) &&
            TryComp<StationRadioServerComponent>(radioServer, out var serverComp))
        {
            StopBroadcast(radioServer, serverComp);
        }
    }

    private void OnVinylInserted(EntityUid uid, VinylPlayerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp(args.Entity, out VinylComponent? vinylcomp) || _net.IsClient || vinylcomp.Song == null || !_power.IsPowered(uid))
            return;

        // Останавливаем все предыдущие звуки
        StopAllSoundsForVinylPlayer(uid, comp);

        // Запускаем новую пластинку
        var audioParams = AudioParams.Default.WithVolume(3f).WithMaxDistance(4.5f).WithLoop(true);
        var audio = _audio.PlayPredicted(vinylcomp.Song, uid, uid, audioParams);
        if (audio != null)
            comp.SoundEntity = audio.Value.Entity;

        comp.InsertedVinyl = args.Entity;

        if (!CheckForRadioServer(uid, out var radioServer) ||
            !TryComp<StationRadioServerComponent>(radioServer, out var serverComp) || serverComp.ChannelId == null)
            return;

        StartBroadcast(radioServer, serverComp, vinylcomp.Song, serverComp.ChannelId);
    }

    private void OnVinylRemove(EntityUid uid, VinylPlayerComponent comp, EntRemovedFromContainerMessage args)
    {
        // Останавливаем ВСЕ звуки
        StopAllSoundsForVinylPlayer(uid, comp);

        comp.InsertedVinyl = null;

        // Останавливаем трансляцию
        if (CheckForRadioServer(uid, out var radioServer) &&
            TryComp<StationRadioServerComponent>(radioServer, out var serverComp))
        {
            StopBroadcast(radioServer, serverComp);
        }
    }

    // ... (твой код без изменений, кроме StartBroadcast)

    private void StartBroadcast(EntityUid radioServer, StationRadioServerComponent serverComp, SoundPathSpecifier song, string channelId)
    {
        if (serverComp.CurrentMedia == song)
            return;

        serverComp.CurrentMedia = song;
        Dirty(radioServer, serverComp);

        var ev = new StationRadioMediaPlayedEvent(song, channelId);
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out var receiverComp))
        {
            if (receiverComp.SelectedChannelId == channelId)
                RaiseLocalEvent(receiver, ev); // broadcast: true по умолчанию — правильно!
        }
    }

    private void StopBroadcast(EntityUid radioServer, StationRadioServerComponent serverComp)
    {
        if (serverComp.CurrentMedia == null || serverComp.ChannelId == null)
            return;

        var channelId = serverComp.ChannelId;
        serverComp.CurrentMedia = null;
        Dirty(radioServer, serverComp);

        var ev = new StationRadioMediaStoppedEvent(channelId);
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out _))
        {
            RaiseLocalEvent(receiver, ev);
        }
    }

    private bool CheckForRadioServer(EntityUid uid, out EntityUid radioServer)
    {
        radioServer = EntityUid.Invalid;

        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return false;

        foreach (var linked in source.LinkedPorts.Keys)
        {
            if (!HasComp<RadioRigComponent>(linked))
                continue;

            if (!TryComp<DeviceLinkSinkComponent>(linked, out var rigSink))
                continue;

            foreach (var serverUid in rigSink.LinkedSources)
            {
                if (HasComp<StationRadioServerComponent>(serverUid))
                {
                    radioServer = serverUid;
                    return true;
                }
            }
        }

        return false;
    }
}