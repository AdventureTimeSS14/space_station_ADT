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
using Robust.Shared.Timing;
using System;

namespace Content.Shared.ADT.StationRadio.Systems;

public sealed class VinylPlayerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StationRadioBroadcastSystem _broadcastSystem = default!;

    // Отслеживаем активные проигрыватели по каналам
    private readonly Dictionary<string, EntityUid> _activePlayersByChannel = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VinylPlayerComponent, EntInsertedIntoContainerMessage>(OnVinylInserted);
        SubscribeLocalEvent<VinylPlayerComponent, EntRemovedFromContainerMessage>(OnVinylRemove);
        SubscribeLocalEvent<VinylPlayerComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<VinylPlayerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<VinylPlayerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnPowerChanged(EntityUid uid, VinylPlayerComponent comp, PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            // При потере питания - останавливаем всё
            StopAllSoundsForVinylPlayer(uid, comp);

            // Останавливаем трансляцию, если есть
            if (TryGetRadioChannel(uid, out var channelId))
            {
                StopBroadcast(channelId, uid);
            }
        }
        else if (args.Powered && comp.InsertedVinyl != null &&
                TryComp<VinylComponent>(comp.InsertedVinyl, out var vinylComp) &&
                vinylComp.Song != null)
        {
            // При восстановлении питания - запускаем пластинку если она есть
            StopAllSoundsForVinylPlayer(uid, comp);
            var audioParams = AudioParams.Default.WithVolume(3f).WithMaxDistance(4.5f).WithLoop(true);
            var audio = _audio.PlayPredicted(vinylComp.Song, uid, null, audioParams);
            if (audio != null)
                comp.SoundEntity = audio.Value.Entity;

            // Запускаем трансляцию, если есть куда транслировать
            if (TryGetRadioChannel(uid, out var channelId))
            {
                StartBroadcast(channelId, vinylComp.Song, uid);
            }
        }
    }

    private void StopAllSoundsForVinylPlayer(EntityUid uid, VinylPlayerComponent comp)
    {
        if (comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value))
        {
            _audio.Stop(comp.SoundEntity.Value);
            comp.SoundEntity = null;
        }
    }

    private void OnDestruction(EntityUid uid, VinylPlayerComponent comp, DestructionEventArgs args)
    {
        StopAllSoundsForVinylPlayer(uid, comp);

        // Останавливаем трансляцию, если есть
        if (TryGetRadioChannel(uid, out var channelId))
        {
            StopBroadcast(channelId, uid);
        }
    }

    private void OnShutdown(EntityUid uid, VinylPlayerComponent comp, ComponentShutdown args)
    {
        StopAllSoundsForVinylPlayer(uid, comp);

        // Останавливаем трансляцию, если есть
        if (TryGetRadioChannel(uid, out var channelId))
        {
            StopBroadcast(channelId, uid);
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
        var audio = _audio.PlayPredicted(vinylcomp.Song, uid, null, audioParams);
        if (audio != null)
            comp.SoundEntity = audio.Value.Entity;

        comp.InsertedVinyl = args.Entity;

        // Запускаем трансляцию, если есть куда транслировать
        if (TryGetRadioChannel(uid, out var channelId))
        {
            StartBroadcast(channelId, vinylcomp.Song, uid);
        }
    }

    private void OnVinylRemove(EntityUid uid, VinylPlayerComponent comp, EntRemovedFromContainerMessage args)
    {
        // Останавливаем ВСЕ звуки
        StopAllSoundsForVinylPlayer(uid, comp);
        comp.InsertedVinyl = null;

        // Останавливаем трансляцию
        if (TryGetRadioChannel(uid, out var channelId))
        {
            StopBroadcast(channelId, uid);
        }
    }

    private void StartBroadcast(string channelId, SoundPathSpecifier song, EntityUid source)
    {
        // Проверяем, не занят ли канал другим проигрывателем
        if (_activePlayersByChannel.TryGetValue(channelId, out var currentPlayer) && currentPlayer != source)
        {
            // Если канал занят другим проигрывателем, останавливаем его
            if (TryComp<VinylPlayerComponent>(currentPlayer, out var otherComp))
            {
                StopAllSoundsForVinylPlayer(currentPlayer, otherComp);
            }
            StopBroadcast(channelId, currentPlayer);
        }

        // Запускаем трансляцию через централизованную систему
        _broadcastSystem.StartBroadcast(channelId, song, source);
        _activePlayersByChannel[channelId] = source;
    }

    private void StopBroadcast(string channelId, EntityUid source)
    {
        // Проверяем, что останавливаем именно тот проигрыватель, который вещает на канале
        if (_activePlayersByChannel.TryGetValue(channelId, out var currentPlayer) && currentPlayer == source)
        {
            _broadcastSystem.StopBroadcast(channelId);
            _activePlayersByChannel.Remove(channelId);
        }
    }

    private bool TryGetRadioChannel(EntityUid uid, out string channelId)
    {
        channelId = string.Empty;

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
                if (TryComp<StationRadioServerComponent>(serverUid, out var serverComp) && serverComp.ChannelId != null)
                {
                    channelId = serverComp.ChannelId;
                    return true;
                }
            }
        }
        return false;
    }
}