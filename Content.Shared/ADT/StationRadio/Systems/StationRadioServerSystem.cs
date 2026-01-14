using Content.Shared.ADT.StationRadio; // Используем общие константы
using Content.Shared.ADT.StationRadio.Components;
using Content.Shared.ADT.StationRadio.Events;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
namespace Content.Shared.ADT.StationRadio.Systems;
/// <summary>
/// Система управления серверами радиостанций.
/// Обрабатывает смену каналов и трансляцию медиа.
/// </summary>
public sealed class StationRadioServerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRadioServerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StationRadioServerComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<StationRadioServerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StationRadioServerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }
    /// <summary>
    /// Обработчик инициализации компонента.
    /// Устанавливает канал по умолчанию, если не задан.
    /// </summary>
    private void OnStartup(EntityUid uid, StationRadioServerComponent comp, ComponentStartup args)
    {
        if (comp.ChannelId == null)
        {
            comp.ChannelId = RadioConstants.DefaultChannel;
            Dirty(uid, comp);
        }
    }
    /// <summary>
    /// Обработчик уничтожения сервера.
    /// Останавливает трансляцию на всех подключенных ресиверах.
    /// </summary>
    private void OnDestruction(EntityUid uid, StationRadioServerComponent comp, DestructionEventArgs args)
    {
        if (comp.CurrentMedia == null || comp.ChannelId == null)
            return;
        var channelId = comp.ChannelId;
        comp.CurrentMedia = null;
        comp.ChannelId = null;
        Dirty(uid, comp);
        var ev = new StationRadioMediaStoppedEvent(channelId);
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out _))
        {
            RaiseLocalEvent(receiver, ev);
        }
    }
    /// <summary>
    /// Обработчик осмотра сервера.
    /// Показывает информацию о текущем канале.
    /// </summary>
    private void OnExamined(EntityUid uid, StationRadioServerComponent comp, ExaminedEvent args)
    {
        if (comp.ChannelId == null || !_proto.TryIndex<RadioChannelPrototype>(comp.ChannelId, out var channel))
        {
            args.PushMarkup(Loc.GetString("station-radio-server-examine-none"));
            return;
        }
        args.PushMarkup(Loc.GetString("station-radio-server-examine", ("channel", channel.LocalizedName)));
    }
    /// <summary>
    /// Обработчик verbs для смены канала сервера.
    /// Предоставляет список доступных каналов для выбора.
        /// </summary>
    private void OnGetVerbs(EntityUid uid, StationRadioServerComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
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
                    if (comp.ChannelId == channelId)
                        return;
                    var oldChannelId = comp.ChannelId;
                    comp.ChannelId = channelId;
                    Dirty(uid, comp);
                    if (comp.CurrentMedia is not { } media)
                        return;
                    // Останавливаем на старой частоте ТОЛЬКО если это другой канал
                    if (oldChannelId != null && oldChannelId != channelId)
                    {
                        var stopEv = new StationRadioMediaStoppedEvent(oldChannelId);
                        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
                        while (query.MoveNext(out var rec, out var recComp))
                        {
                            // Отправляем событие ТОЛЬКО тем ресиверам, которые слушают этот канал
                            if (recComp.SelectedChannelId == oldChannelId)
                                RaiseLocalEvent(rec, stopEv);
                        }
                    }
                    // Запускаем на новой - ОДИН РАЗ для каждого ресивера
                    var playEv = new StationRadioMediaPlayedEvent(media, channelId);
                    var queryPlay = EntityQueryEnumerator<StationRadioReceiverComponent>();
                    while (queryPlay.MoveNext(out var rec, out var recComp))
                    {
                        if (recComp.SelectedChannelId == channelId)
                        {
                            // Отправляем событие напрямую, а не broadcast
                            RaiseLocalEvent(rec, playEv);
                        }
                    }
                }
            };
            args.Verbs.Add(verb);
        }
    }
}
