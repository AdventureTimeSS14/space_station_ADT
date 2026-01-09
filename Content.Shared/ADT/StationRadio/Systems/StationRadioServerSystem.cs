using Content.Shared.ADT.StationRadio.Components;
using Content.Shared.ADT.StationRadio.Events;
using Content.Shared.DeviceLinking;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.StationRadio.Systems;

public sealed class StationRadioServerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRadioServerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid uid, StationRadioServerComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        foreach (var channel in _proto.EnumeratePrototypes<RadioChannelPrototype>())
        {
            var channelId = channel.ID; // Capture in closure
            var verb = new Verb
            {
                Category = VerbCategory.StationRadio,
                Text = channel.LocalizedName,
                Icon = new SpriteSpecifier.Texture(new("/Textures/ADT/Interface/VerbIcons/icons-radio.svg.png")), // Optional, adjust path if needed
                Act = () =>
                {
                    var oldChannelId = comp.ChannelId;

                    if (oldChannelId == channelId)
                        return;

                    comp.ChannelId = channelId;
                    Dirty(uid, comp);

                    // Handle switching music if playing
                    if (!TryComp<DeviceLinkSourceComponent>(uid, out var serverSrc))
                        return;

                    foreach (var rig in serverSrc.LinkedPorts.Keys)
                    {
                        if (!HasComp<RadioRigComponent>(rig))
                            continue;

                        if (!TryComp<DeviceLinkSinkComponent>(rig, out var rigSink))
                            continue;

                        foreach (var vinyl in rigSink.LinkedSources)
                        {
                            if (!TryComp<VinylPlayerComponent>(vinyl, out var vinylComp) || !vinylComp.RelayToRadios || vinylComp.SoundEntity == null || vinylComp.InsertedVinyl == null)
                                continue;

                            if (!TryComp<VinylComponent>(vinylComp.InsertedVinyl, out var vComp) || vComp.Song == null)
                                continue;

                            // Stop on old channel
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

                            // Play on new channel
                            var playEv = new StationRadioMediaPlayedEvent(vComp.Song, channelId);
                            var queryPlay = EntityQueryEnumerator<StationRadioReceiverComponent>();
                            while (queryPlay.MoveNext(out var rec, out var recComp))
                            {
                                if (recComp.SelectedChannelId == channelId && !recComp.SoundEntity.HasValue)
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