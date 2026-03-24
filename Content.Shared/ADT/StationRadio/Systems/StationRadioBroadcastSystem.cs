using Content.Shared.ADT.StationRadio.Components;
using Content.Shared.ADT.StationRadio.Events;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.ADT.StationRadio.Systems;

/// <summary>
/// Централизованная система управления радио-трансляциями.
/// Отслеживает активные трансляции и управляет подпиской ресиверов.
/// </summary>
public sealed class StationRadioBroadcastSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <summary>
    /// Активные трансляции по каналам
    /// </summary>
    private readonly Dictionary<string, ActiveBroadcast> _activeBroadcasts = new();

    /// <summary>
    /// Ресиверы, подписанные на каналы
    /// </summary>
    private readonly Dictionary<string, HashSet<EntityUid>> _channelSubscribers = new();

    /// <summary>
    /// Каналы, на которые подписан каждый ресивер
    /// </summary>
    private readonly Dictionary<EntityUid, string> _receiverChannels = new();

    public override void Initialize()
    {
        base.Initialize();

        // УБИРАЕМ ПОДПИСКИ НА СОБЫТИЯ РЕСИВЕРА - они теперь будут в StationRadioReceiverSystem
        // SubscribeLocalEvent<StationRadioReceiverComponent, ComponentStartup>(OnReceiverStartup);
        // SubscribeLocalEvent<StationRadioReceiverComponent, ComponentShutdown>(OnReceiverShutdown);
        // SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioChannelChangedEvent>(OnReceiverChannelChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Обновляем время прошедшее для всех трансляций
        var currentTime = _gameTiming.CurTime;
        var toRemove = new List<string>();

        foreach (var (channelId, broadcast) in _activeBroadcasts)
        {
            // Проверяем, не истекло ли время трансляции (если есть длительность)
            if (broadcast.EndTime.HasValue && currentTime > broadcast.EndTime.Value)
            {
                toRemove.Add(channelId);
            }
        }

        // Удаляем истекшие трансляции
        foreach (var channelId in toRemove)
        {
            StopBroadcast(channelId);
        }
    }

    /// <summary>
    /// Запускает трансляцию на канале
    /// </summary>
    public void StartBroadcast(string channelId, SoundPathSpecifier media, EntityUid? source = null)
    {
        if (_activeBroadcasts.ContainsKey(channelId))
        {
            // Если на канале уже есть трансляция, останавливаем её
            StopBroadcast(channelId);
        }

        var broadcast = new ActiveBroadcast
        {
            ChannelId = channelId,
            Media = media,
            StartTime = _gameTiming.CurTime,
            Source = source,
            BroadcastId = Guid.NewGuid()
        };

        _activeBroadcasts[channelId] = broadcast;

        // Уведомляем всех подписчиков канала
        NotifySubscribers(channelId, broadcast);
    }

    /// <summary>
    /// Останавливает трансляцию на канале
    /// </summary>
    public void StopBroadcast(string channelId)
    {
        if (!_activeBroadcasts.Remove(channelId, out var broadcast))
            return;

        // Уведомляем всех подписчиков об остановке
        if (_channelSubscribers.TryGetValue(channelId, out var subscribers))
        {
            foreach (var receiver in subscribers)
            {
                var ev = new StationRadioMediaStoppedEvent(channelId);
                RaiseLocalEvent(receiver, ev);
            }
        }
    }

    /// <summary>
    /// Подписывает ресивер на канал
    /// </summary>
    public void SubscribeToChannel(EntityUid receiver, string channelId, StationRadioReceiverComponent? comp = null)
    {
        if (!Resolve(receiver, ref comp, false))
            return;

        // Отписываем от старого канала, если есть
        if (_receiverChannels.TryGetValue(receiver, out var oldChannel) && oldChannel != channelId)
        {
            UnsubscribeFromChannel(receiver, oldChannel);
        }

        // Подписываем на новый канал
        if (!_channelSubscribers.ContainsKey(channelId))
        {
            _channelSubscribers[channelId] = new HashSet<EntityUid>();
        }

        _channelSubscribers[channelId].Add(receiver);
        _receiverChannels[receiver] = channelId;

        // Если на канале есть активная трансляция, уведомляем ресивер
        if (_activeBroadcasts.TryGetValue(channelId, out var broadcast))
        {
            NotifyReceiver(receiver, broadcast);
        }
    }

    /// <summary>
    /// Отписывает ресивер от канала
    /// </summary>
    public void UnsubscribeFromChannel(EntityUid receiver, string channelId)
    {
        if (_channelSubscribers.TryGetValue(channelId, out var subscribers))
        {
            subscribers.Remove(receiver);

            if (subscribers.Count == 0)
            {
                _channelSubscribers.Remove(channelId);
            }
        }

        _receiverChannels.Remove(receiver);

        // Отправляем событие остановки ресиверу
        var ev = new StationRadioMediaStoppedEvent(channelId);
        RaiseLocalEvent(receiver, ev);
    }

    /// <summary>
    /// Получает информацию о текущей трансляции на канале
    /// </summary>
    public ActiveBroadcast? GetCurrentBroadcast(string channelId)
    {
        return _activeBroadcasts.GetValueOrDefault(channelId);
    }

    /// <summary>
    /// Проверяет, активна ли трансляция на канале
    /// </summary>
    public bool IsBroadcastActive(string channelId)
    {
        return _activeBroadcasts.ContainsKey(channelId);
    }

    /// <summary>
    /// Полностью останавливает все трансляции и очищает подписки
    /// </summary>
    public void StopAllBroadcasts()
    {
        foreach (var channelId in _activeBroadcasts.Keys.ToList())
        {
            StopBroadcast(channelId);
        }

        _activeBroadcasts.Clear();
        _channelSubscribers.Clear();
        _receiverChannels.Clear();
    }

    private void NotifySubscribers(string channelId, ActiveBroadcast broadcast)
    {
        if (!_channelSubscribers.TryGetValue(channelId, out var subscribers))
            return;

        foreach (var receiver in subscribers)
        {
            NotifyReceiver(receiver, broadcast);
        }
    }

    private void NotifyReceiver(EntityUid receiver, ActiveBroadcast broadcast)
    {
        var ev = new StationRadioMediaPlayedEvent(
            broadcast.Media,
            broadcast.ChannelId,
            broadcast.StartTime,
            broadcast.BroadcastId);

        RaiseLocalEvent(receiver, ev);
    }

    /// <summary>
    /// Информация об активной трансляции
    /// </summary>
    public sealed class ActiveBroadcast
    {
        public string ChannelId { get; set; } = default!;
        public SoundPathSpecifier Media { get; set; } = default!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public EntityUid? Source { get; set; }
        public Guid BroadcastId { get; set; }
    }
}