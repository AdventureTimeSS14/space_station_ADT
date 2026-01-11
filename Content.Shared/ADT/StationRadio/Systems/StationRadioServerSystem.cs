using Content.Shared.ADT.StationRadio.Components;
using Content.Shared.ADT.StationRadio.Events;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.StationRadio.Systems;

public sealed class StationRadioServerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private const string DefaultChannel = "ADTOldBroadcast";

    private readonly string[] _allowedChannels = new[] { "ADTOldBroadcast", "ADTOldBroadcast2", "ADTOldBroadcast3" };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationRadioServerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StationRadioServerComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<StationRadioServerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StationRadioServerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnStartup(EntityUid uid, StationRadioServerComponent comp, ComponentStartup args)
    {
        if (comp.ChannelId == null)
        {
            comp.ChannelId = DefaultChannel;
            Dirty(uid, comp);
        }
    }

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
        while (query.MoveNext(out var receiver, out var receiverComp))
        {
            if (receiverComp.SelectedChannelId == channelId)
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

        foreach (var id in _allowedChannels)
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

                    if (oldChannelId != null)
                    {
                        var stopEv = new StationRadioMediaStoppedEvent(oldChannelId);
                        var query = EntityQueryEnumerator<StationRadioReceiverComponent>();
                        while (query.MoveNext(out var rec, out var recComp))
                        {
                            if (recComp.SelectedChannelId == oldChannelId)
                                RaiseLocalEvent(rec, stopEv);
                        }
                    }

                    var playEv = new StationRadioMediaPlayedEvent(media, channelId);
                    var queryPlay = EntityQueryEnumerator<StationRadioReceiverComponent>();
                    while (queryPlay.MoveNext(out var rec, out var recComp))
                    {
                        if (recComp.SelectedChannelId == channelId)
                            RaiseLocalEvent(rec, playEv);
                    }
                }
            };
            args.Verbs.Add(verb);
        }
    }
}
