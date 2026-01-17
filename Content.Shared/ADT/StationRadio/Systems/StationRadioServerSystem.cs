using Content.Shared.ADT.StationRadio;
using Content.Shared.ADT.StationRadio.Components;
using Content.Shared.ADT.StationRadio.Events;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.StationRadio.Systems;

public sealed class StationRadioServerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRadioServerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StationRadioServerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<StationRadioServerComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<StationRadioServerComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<StationRadioServerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StationRadioServerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetState(EntityUid uid, StationRadioServerComponent comp, ref ComponentGetState args)
    {
        args.State = new StationRadioServerComponentState(
            comp.ChannelId,
            comp.CurrentMedia,
            comp.BroadcastStartTime,
            comp.CurrentBroadcastId);
    }

    private void OnHandleState(EntityUid uid, StationRadioServerComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not StationRadioServerComponentState state)
            return;

        comp.ChannelId = state.ChannelId;
        comp.CurrentMedia = state.CurrentMedia;
        comp.BroadcastStartTime = state.BroadcastStartTime;
        comp.CurrentBroadcastId = state.CurrentBroadcastId;
    }

    private void OnStartup(EntityUid uid, StationRadioServerComponent comp, ComponentStartup args)
    {
        if (comp.ChannelId == null)
        {
            comp.ChannelId = RadioConstants.DefaultChannel;
            Dirty(uid, comp);
        }
    }

    private void OnDestruction(EntityUid uid, StationRadioServerComponent comp, DestructionEventArgs args)
    {
        if (comp.CurrentMedia == null || comp.ChannelId == null)
            return;

        var channelId = comp.ChannelId;
        comp.CurrentMedia = null;
        comp.BroadcastStartTime = null;
        comp.CurrentBroadcastId = null;
        Dirty(uid, comp);

        var ev = new StationRadioMediaStoppedEvent(channelId);
        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
        while (query.MoveNext(out var receiver, out _))
        {
            RaiseLocalEvent(receiver, ev);
        }
    }

    private void OnExamined(EntityUid uid, StationRadioServerComponent comp, ExaminedEvent args)
    {
        if (comp.ChannelId == null || !_proto.TryIndex<RadioChannelPrototype>(comp.ChannelId, out var channel))
        {
            args.PushMarkup(Loc.GetString("station-radio-server-examine-none"));
            return;
        }
        args.PushMarkup(Loc.GetString("station-radio-server-examine", ("channel", channel.LocalizedName)));
    }

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

                    // Если меняем канал с активным вещанием, останавливаем на старом
                    if (oldChannelId != null && oldChannelId != channelId)
                    {
                        var stopEv = new StationRadioMediaStoppedEvent(oldChannelId);
                        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
                        while (query.MoveNext(out var rec, out var recComp))
                        {
                            if (recComp.SelectedChannelId == oldChannelId)
                                RaiseLocalEvent(rec, stopEv);
                        }
                    }

                    // Запускаем на новом канале с тем же треком (если он играет)
                    if (comp.BroadcastStartTime != null && comp.CurrentBroadcastId != null)
                    {
                        var playEv = new StationRadioMediaPlayedEvent(
                            media,
                            channelId,
                            comp.BroadcastStartTime.Value,
                            comp.CurrentBroadcastId.Value);

                        var queryPlay = EntityQueryEnumerator<StationRadioReceiverComponent>();
                        while (queryPlay.MoveNext(out var rec, out var recComp))
                        {
                            if (recComp.SelectedChannelId == channelId)
                            {
                                RaiseLocalEvent(rec, playEv);
                            }
                        }
                    }
                }
            };
            args.Verbs.Add(verb);
        }
    }
}