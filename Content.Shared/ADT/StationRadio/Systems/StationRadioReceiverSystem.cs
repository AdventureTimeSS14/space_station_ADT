using Content.Shared.ADT.StationRadio;
using Content.Shared.ADT.StationRadio.Components;
using Content.Shared.ADT.StationRadio.Events;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared.Radio.Components;

namespace Content.Shared.ADT.StationRadio.Systems;
public sealed class StationRadioReceiverSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    // Трекер для предотвращения множественных вызовов одного события
    private readonly Dictionary<EntityUid, (string ChannelId, SoundPathSpecifier? Media, TimeSpan LastCall)> _eventTracker = new();
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRadioReceiverComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaPlayedEvent>(OnMediaPlayed);
        SubscribeLocalEvent<StationRadioReceiverComponent, StationRadioMediaStoppedEvent>(OnMediaStopped);
        SubscribeLocalEvent<StationRadioReceiverComponent, ActivateInWorldEvent>(OnRadioToggle);
        SubscribeLocalEvent<StationRadioReceiverComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationRadioReceiverComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        CleanupEventTracker();
    }
    private void CleanupEventTracker()
    {
        var currentTime = _gameTiming.CurTime;
        var toRemove = new List<EntityUid>();
        foreach (var (uid, (_, _, lastCall)) in _eventTracker)
        {
            if (currentTime - lastCall > TimeSpan.FromSeconds(1))
                toRemove.Add(uid);
        }
        foreach (var uid in toRemove)
            _eventTracker.Remove(uid);
    }
    private void OnStartup(EntityUid uid, StationRadioReceiverComponent comp, ComponentStartup args)
    {
        if (comp.SelectedChannelId == null)
        {
            comp.SelectedChannelId = RadioConstants.DefaultChannel;
            Dirty(uid, comp);
        }
        TryJoinCurrentBroadcast(uid, comp);
    }
    private void OnPowerChanged(EntityUid uid, StationRadioReceiverComponent comp, PowerChangedEvent args)
    {
        if (comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value) &&
            TryComp<AudioComponent>(comp.SoundEntity.Value, out var audioComp))
        {
            var gain = args.Powered ? (comp.Active ? 1f : 0f) : 0f;
            _audio.SetGain(comp.SoundEntity.Value, gain, audioComp);
        }
        else if (!args.Powered)
        {
            StopMedia(uid, comp);
        }
        if (args.Powered)
            TryJoinCurrentBroadcast(uid, comp);
    }
    private void OnRadioToggle(EntityUid uid, StationRadioReceiverComponent comp, ActivateInWorldEvent args)
    {
        comp.Active = !comp.Active;
        if (comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value) &&
            TryComp<AudioComponent>(comp.SoundEntity.Value, out var audioComp) && _power.IsPowered(uid))
        {
            var gain = comp.Active ? 1f : 0f;
            _audio.SetGain(comp.SoundEntity.Value, gain, audioComp);
        }
        if (!comp.Active)
            StopMedia(uid, comp);
    }
    private void OnMediaPlayed(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaPlayedEvent args)
    {
        // Проверяем, не обрабатывали ли мы уже это событие для этого ресивера
        var eventKey = (uid, args.ChannelId, args.MediaPlayed);
        var currentTime = _gameTiming.CurTime;
        if (_eventTracker.TryGetValue(uid, out var tracked) &&
            tracked.ChannelId == args.ChannelId &&
            tracked.Media == args.MediaPlayed &&
            currentTime - tracked.LastCall < TimeSpan.FromMilliseconds(500))
        {
            return; // Игнорируем повторное событие
        }
        _eventTracker[uid] = (args.ChannelId, args.MediaPlayed, currentTime);
        if (comp.SelectedChannelId != args.ChannelId)
            return;
        PlayMedia(uid, comp, args.MediaPlayed);
    }
    private void PlayMedia(EntityUid uid, StationRadioReceiverComponent comp, SoundPathSpecifier media)
    {
        // Если уже играет та же музыка - ничего не делаем
        if (comp.CurrentMedia == media && comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value))
            return;
        // Всегда останавливаем предыдущий звук
        StopMedia(uid, comp);
        if (!comp.Active || !_power.IsPowered(uid))
            return;
        var audioParams = AudioParams.Default
            .WithVolume(3f)
            .WithMaxDistance(4.5f)
            .WithLoop(true)
            .WithVariation(0f); // Отключаем вариации для точного сравнения
        // Воспроизводим только если мы на сервере ИЛИ это предсказание на клиенте
        if (_netManager.IsServer || !_netManager.IsConnected)
        {
            var audio = _audio.PlayPredicted(media, uid, null, audioParams);
            if (audio == null)
                return;
            comp.SoundEntity = audio.Value.Entity;
            comp.CurrentMedia = media;
        }
    }
    private void StopMedia(EntityUid uid, StationRadioReceiverComponent comp)
    {
        if (comp.SoundEntity.HasValue && Exists(comp.SoundEntity.Value))
        {
            _audio.Stop(comp.SoundEntity.Value);
        }
        comp.SoundEntity = null;
        comp.CurrentMedia = null;
    }
    private void OnMediaStopped(EntityUid uid, StationRadioReceiverComponent comp, StationRadioMediaStoppedEvent args)
    {
        if (comp.SelectedChannelId != args.ChannelId)
            return;
        StopMedia(uid, comp);
    }
    private void OnGetVerbs(EntityUid uid, StationRadioReceiverComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // НОВАЯ ПРОВЕРКА: если есть новый универсальный компонент — не добавляем свои verbs
        if (HasComp<VerbSelectableRadioChannelComponent>(uid))
            return;
        foreach (var id in RadioConstants.AllowedChannels)
        {
            if (!_proto.TryIndex<RadioChannelPrototype>(id, out var channel))
                continue;
            var channelId = channel.ID;
            var verb = new Verb
            {
                Category = VerbCategory.StationRadio,
                Text = channel.LocalizedName,
                Act = () =>
                {
                    if (comp.SelectedChannelId == channelId)
                        return;
                    // Останавливаем текущий звук
                    StopMedia(uid, comp);
                    comp.SelectedChannelId = channelId;
                    Dirty(uid, comp);
                    // Пытаемся присоединиться к вещанию на новом канале
                    TryJoinCurrentBroadcast(uid, comp);
                }
            };
            args.Verbs.Add(verb);
        }
    }
    private void TryJoinCurrentBroadcast(EntityUid uid, StationRadioReceiverComponent comp)
    {
        if (comp.SelectedChannelId == null || !_power.IsPowered(uid) || !comp.Active)
            return;
        if (GetCurrentBroadcastMedia(comp.SelectedChannelId) is { } media)
        {
            var ev = new StationRadioMediaPlayedEvent(media, comp.SelectedChannelId);
            RaiseLocalEvent(uid, ev);
        }
    }
    private SoundPathSpecifier? GetCurrentBroadcastMedia(string? channelId)
    {
        if (channelId == null)
            return null;
        var query = EntityQueryEnumerator<StationRadioServerComponent>();
        while (query.MoveNext(out _, out var serverComp))
        {
            if (serverComp.ChannelId == channelId && serverComp.CurrentMedia != null)
                return serverComp.CurrentMedia;
        }
        return null;
    }
}
