using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.ADT.Language;  // ADT Languages
using Robust.Shared.Audio.Systems; // ADT Radio Update
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.Components;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Radio;
using Content.Shared.Chat;
using Content.Shared.Radio.Components;
using Content.Shared.Verbs;
using Content.Shared.ADT.StationRadio.Components; // ADT-Tweak
using Content.Shared.ADT.StationRadio.Systems; // ADT-Tweak
using Content.Shared.ADT.StationRadio.Events; // ADT-Tweak
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;  // ADT-Tweak

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles radio speakers and microphones (which together form a hand-held radio).
/// </summary>
public sealed class RadioDeviceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly LanguageSystem _language = default!;  // ADT Languages
    [Dependency] private readonly SharedAudioSystem _audio = default!;  // ADT Radio Update
    [Dependency] private readonly StationRadioBroadcastSystem _broadcastSystem = default!;  // ADT-Tweak

    private HashSet<(string, EntityUid, RadioChannelPrototype)> _recentlySent = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioMicrophoneComponent, ComponentInit>(OnMicrophoneInit);
        SubscribeLocalEvent<RadioMicrophoneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RadioMicrophoneComponent, ActivateInWorldEvent>(OnActivateMicrophone);
        SubscribeLocalEvent<RadioMicrophoneComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<RadioMicrophoneComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<RadioMicrophoneComponent, PowerChangedEvent>(OnMicrophonePowerChanged);

        SubscribeLocalEvent<RadioSpeakerComponent, ComponentInit>(OnSpeakerInit);
        SubscribeLocalEvent<RadioSpeakerComponent, ActivateInWorldEvent>(OnActivateSpeaker);
        SubscribeLocalEvent<RadioSpeakerComponent, RadioReceiveEvent>(OnReceiveRadio);
        SubscribeLocalEvent<RadioSpeakerComponent, PowerChangedEvent>(OnSpeakerPowerChanged);

        SubscribeLocalEvent<IntercomComponent, EncryptionChannelsChangedEvent>(OnIntercomEncryptionChannelsChanged);
        SubscribeLocalEvent<IntercomComponent, ToggleIntercomMicMessage>(OnToggleIntercomMic);
        SubscribeLocalEvent<IntercomComponent, ToggleIntercomSpeakerMessage>(OnToggleIntercomSpeaker);
        SubscribeLocalEvent<IntercomComponent, SelectIntercomChannelMessage>(OnSelectIntercomChannel);

        // Start ADT-Tweak
        SubscribeLocalEvent<VerbSelectableRadioChannelComponent, ComponentInit>(OnChannelComponentInit);
        SubscribeLocalEvent<VerbSelectableRadioChannelComponent, ComponentGetState>(OnChannelGetState);
        SubscribeLocalEvent<VerbSelectableRadioChannelComponent, ComponentHandleState>(OnChannelHandleState);
        SubscribeLocalEvent<VerbSelectableRadioChannelComponent, GetVerbsEvent<Verb>>(OnGetChannelVerbs);
        SubscribeLocalEvent<VerbSelectableRadioChannelComponent, ExaminedEvent>(OnChannelExamined);
        SubscribeLocalEvent<VerbSelectableRadioChannelComponent, StationRadioChannelChangedEvent>(OnChannelComponentChanged);
        // End ADT-Tweak
    }

    // Start ADT-Tweak
    private void OnChannelGetState(EntityUid uid, VerbSelectableRadioChannelComponent comp, ref ComponentGetState args)
    {
        args.State = new VerbSelectableRadioChannelComponentState(comp.SelectedChannelId);
    }

    private void OnChannelHandleState(EntityUid uid, VerbSelectableRadioChannelComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not VerbSelectableRadioChannelComponentState state)
            return;

        if (comp.SelectedChannelId != state.SelectedChannelId)
        {
            comp.SelectedChannelId = state.SelectedChannelId;
            UpdateChannelOnComponents(uid, comp.SelectedChannelId);
        }
    }
    // End ADT-Tweak

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _recentlySent.Clear();
    }

    #region Component Init
    private void OnMicrophoneInit(EntityUid uid, RadioMicrophoneComponent component, ComponentInit args)
    {
        if (component.Enabled)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }

    private void OnSpeakerInit(EntityUid uid, RadioSpeakerComponent component, ComponentInit args)
    {
        UpdateSpeakerActiveChannels(uid, component);    // ADT-Tweak
    }
    #endregion

    #region Toggling
    private void OnActivateMicrophone(EntityUid uid, RadioMicrophoneComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex || !component.ToggleOnInteract) // ADT-Tweak
            return;

        ToggleRadioMicrophone(uid, args.User, args.Handled, component);
        args.Handled = true;
    }

    private void OnActivateSpeaker(EntityUid uid, RadioSpeakerComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex || !component.ToggleOnInteract)  // ADT-Tweak
            return;

        ToggleRadioSpeaker(uid, args.User, args.Handled, component);
        args.Handled = true;
    }

    public void ToggleRadioMicrophone(EntityUid uid, EntityUid user, bool quiet = false, RadioMicrophoneComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetMicrophoneEnabled(uid, user, !component.Enabled, quiet, component);
    }

    // Start ADT-Tweak
    public void ToggleRadioSpeaker(EntityUid uid, EntityUid user, bool quiet = false, RadioSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        SetSpeakerEnabled(uid, user, !component.Enabled, quiet, component);
    }

    #region Power Handling
    private void OnMicrophonePowerChanged(EntityUid uid, RadioMicrophoneComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered || !component.PowerRequired)
            return;

        SetMicrophoneEnabled(uid, null, false, true, component);
    }

    private void OnSpeakerPowerChanged(EntityUid uid, RadioSpeakerComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered || !component.PowerRequired)
            return;

        SetSpeakerEnabled(uid, null, false, true, component);
    }
    #endregion
    // End ADT-Tweak

    public void SetMicrophoneEnabled(EntityUid uid, EntityUid? user, bool enabled, bool quiet = false, RadioMicrophoneComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.PowerRequired && !this.IsPowered(uid, EntityManager))
        // Start ADT-Tweak
        {
            if (enabled && user != null)
                _popup.PopupEntity(Loc.GetString("handheld-radio-component-no-power"), uid, user.Value);
            return;
        }
        // Start ADT-Tweak
        component.Enabled = enabled;

        if (!quiet && user != null)
        {
            var state = Loc.GetString(component.Enabled ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user.Value, user.Value);
        }

        _appearance.SetData(uid, RadioDeviceVisuals.Broadcasting, component.Enabled);

        if (component.Enabled)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }

    public void SetSpeakerEnabled(EntityUid uid, EntityUid? user, bool enabled, bool quiet = false, RadioSpeakerComponent? component = null)  // ADT-Tweak
    {
        if (!Resolve(uid, ref component))
            return;

        // Start ADT-Tweak
        if (component.PowerRequired && !this.IsPowered(uid, EntityManager))
        {
            if (enabled && user != null)
                _popup.PopupEntity(Loc.GetString("handheld-radio-component-no-power"), uid, user.Value);
            return;
        }

        var wasEnabled = component.Enabled;
        // End ADT-Tweak
        component.Enabled = enabled;

        if (!quiet && user != null)
        {
            var state = Loc.GetString(component.Enabled ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state");
            var message = Loc.GetString("handheld-radio-component-on-use", ("radioState", state));
            _popup.PopupEntity(message, user.Value, user.Value);
        }

        _appearance.SetData(uid, RadioDeviceVisuals.Speaker, component.Enabled);
        // Start ADT-Tweak
        if (component.Enabled || wasEnabled)
            UpdateSpeakerActiveChannels(uid, component);
    }

    private void UpdateSpeakerActiveChannels(EntityUid uid, RadioSpeakerComponent component)
    {
        if (component.Enabled && (!component.PowerRequired || this.IsPowered(uid, EntityManager)))
        {
            var activeComp = EnsureComp<ActiveRadioComponent>(uid);
            activeComp.Channels.Clear();
            activeComp.Channels.UnionWith(component.Channels);
        }
        else
        {
            RemCompDeferred<ActiveRadioComponent>(uid);
        }
    }
        // End ADT-Tweak
    #endregion

    private void OnExamine(EntityUid uid, RadioMicrophoneComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proto = _protoMan.Index<RadioChannelPrototype>(component.BroadcastChannel);

        using (args.PushGroup(nameof(RadioMicrophoneComponent)))
        {
            args.PushMarkup(Loc.GetString("handheld-radio-component-on-examine", ("frequency", proto.Frequency)));
            args.PushMarkup(Loc.GetString("handheld-radio-component-chennel-examine",
                ("channel", proto.LocalizedName)));
        }
    }

    private void OnListen(EntityUid uid, RadioMicrophoneComponent component, ListenEvent args)
    {
        if (HasComp<RadioSpeakerComponent>(args.Source))
            return; // no feedback loops please.

        var channel = _protoMan.Index<RadioChannelPrototype>(component.BroadcastChannel)!;
        if (_recentlySent.Add((args.Message, args.Source, channel)))
            _radio.SendRadioMessage(args.Source, args.Message, channel, uid);
    }

    private void OnAttemptListen(EntityUid uid, RadioMicrophoneComponent component, ListenAttemptEvent args)
    {
    // Start ADT-Tweak
        if (component.PowerRequired && !this.IsPowered(uid, EntityManager) ||
            component.UnobstructedRequired && !_interaction.InRangeUnobstructed(args.Source, uid, 0))
    // End ADT-Tweak
        {
            args.Cancel();
        }
    }

    private void OnReceiveRadio(EntityUid uid, RadioSpeakerComponent component, ref RadioReceiveEvent args)
    {
        if (uid == args.RadioSource)
            return;

        // Start ADT-Tweak
        if (!component.Enabled || (component.PowerRequired && !this.IsPowered(uid, EntityManager)))
            return;
        // End ADT-Tweak

        if (component.SoundOnReceive != null)
            _audio.PlayPvs(component.SoundOnReceive, uid);

        var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
        RaiseLocalEvent(args.MessageSource, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(uid)),
            ("originalName", nameEv.VoiceName));

        var chatType = component.SpeakNormally ? InGameICChatType.Speak : InGameICChatType.Whisper;

        _chat.TrySendInGameICMessage(
            uid,
            args.Message,
            chatType,   // ADT-Tweak
            ChatTransmitRange.GhostRangeLimit,
            nameOverride: name,
            checkRadioPrefix: false,
            language: args.Language
        );
    }

    private void OnIntercomEncryptionChannelsChanged(Entity<IntercomComponent> ent, ref EncryptionChannelsChangedEvent args)
    {
        ent.Comp.SupportedChannels = args.Component.Channels.Select(p => new ProtoId<RadioChannelPrototype>(p)).ToList();

        var channel = args.Component.DefaultChannel;
        if (ent.Comp.CurrentChannel != null && ent.Comp.SupportedChannels.Contains(ent.Comp.CurrentChannel.Value))
            channel = ent.Comp.CurrentChannel;

        SetIntercomChannel(ent, channel);
    }

    private void OnToggleIntercomMic(Entity<IntercomComponent> ent, ref ToggleIntercomMicMessage args)
    {
        if (ent.Comp.RequiresPower && !this.IsPowered(ent, EntityManager))
            return;

        SetMicrophoneEnabled(ent, args.Actor, args.Enabled, true);
        ent.Comp.MicrophoneEnabled = args.Enabled;
        Dirty(ent);
    }

    private void OnToggleIntercomSpeaker(Entity<IntercomComponent> ent, ref ToggleIntercomSpeakerMessage args)
    {
        if (ent.Comp.RequiresPower && !this.IsPowered(ent, EntityManager))
            return;

        SetSpeakerEnabled(ent, args.Actor, args.Enabled, true);
        ent.Comp.SpeakerEnabled = args.Enabled;
        Dirty(ent);
    }

    private void OnSelectIntercomChannel(Entity<IntercomComponent> ent, ref SelectIntercomChannelMessage args)
    {
        if (ent.Comp.RequiresPower && !this.IsPowered(ent, EntityManager))
            return;

        if (!_protoMan.HasIndex<RadioChannelPrototype>(args.Channel) || !ent.Comp.SupportedChannels.Contains(args.Channel))
            return;

        SetIntercomChannel(ent, args.Channel);
    }

    private void SetIntercomChannel(Entity<IntercomComponent> ent, ProtoId<RadioChannelPrototype>? channel)
    {
        ent.Comp.CurrentChannel = channel;

        if (channel == null)
        {
            SetSpeakerEnabled(ent, null, false);
            SetMicrophoneEnabled(ent, null, false);
            ent.Comp.MicrophoneEnabled = false;
            ent.Comp.SpeakerEnabled = false;
            Dirty(ent);
            return;
        }

        if (TryComp<RadioMicrophoneComponent>(ent, out var mic))
            mic.BroadcastChannel = channel;
        if (TryComp<RadioSpeakerComponent>(ent, out var speaker))
            speaker.Channels = new() { channel };
        Dirty(ent);
    }
        // Start ADT-Tweak
    #region VerbSelectableRadioChannel

    private void OnChannelComponentInit(EntityUid uid, VerbSelectableRadioChannelComponent comp, ComponentInit args)
    {
        var current = comp.SelectedChannelId;

        if (TryComp<StationRadioReceiverComponent>(uid, out var receiver) && receiver.SelectedChannelId != null)
            current = receiver.SelectedChannelId;
        else if (TryComp<RadioMicrophoneComponent>(uid, out var mic) && !string.IsNullOrEmpty(mic.BroadcastChannel))
            current = mic.BroadcastChannel;
        else if (TryComp<RadioSpeakerComponent>(uid, out var speaker) && speaker.Channels.Count > 0)
            current = speaker.Channels.First();

        if (current != comp.SelectedChannelId)
        {
            comp.SelectedChannelId = current;
            Dirty(uid, comp);
        }

        // Синхронизируем все компоненты с выбранным каналом
        UpdateChannelOnComponents(uid, comp.SelectedChannelId);
    }

    private void OnChannelComponentChanged(EntityUid uid, VerbSelectableRadioChannelComponent comp, StationRadioChannelChangedEvent args)
    {
        if (comp.SelectedChannelId == args.NewChannelId)
            return;

        comp.SelectedChannelId = args.NewChannelId;
        Dirty(uid, comp);

        // Обновляем все компоненты на новом канале
        UpdateChannelOnComponents(uid, args.NewChannelId);
        // End ADT-Tweak
    }

    private void OnGetChannelVerbs(EntityUid uid, VerbSelectableRadioChannelComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<RadioMicrophoneComponent>(uid) && !HasComp<RadioSpeakerComponent>(uid) && !HasComp<StationRadioReceiverComponent>(uid))
            return;

        var channelIds = comp.AllowedChannelIds.Count > 0
            ? comp.AllowedChannelIds
            : _protoMan.EnumeratePrototypes<RadioChannelPrototype>().Select(p => p.ID).ToList();

        foreach (var id in channelIds)
        {
            if (!_protoMan.TryIndex<RadioChannelPrototype>(id, out var channelProto))
                continue;

            args.Verbs.Add(new Verb
            {
                Category = VerbCategory.StationRadio,
                Text = channelProto.LocalizedName,
                Disabled = comp.SelectedChannelId == id,
                Act = () => SetRadioChannel(uid, id, args.User)
            });
        }
    }

    private void SetRadioChannel(EntityUid uid, string channelId, EntityUid user)
    {
        if (!TryComp<VerbSelectableRadioChannelComponent>(uid, out var comp))
            return;

        if (comp.SelectedChannelId == channelId)
            return;

        // Обновляем состояние компонента
        comp.SelectedChannelId = channelId;
        Dirty(uid, comp);

        // Отправляем событие изменения канала для всех заинтересованных компонентов
        var ev = new StationRadioChannelChangedEvent(channelId);
        RaiseLocalEvent(uid, ev);

        // Обновляем все компоненты
        UpdateChannelOnComponents(uid, channelId);

        if (_protoMan.TryIndex<RadioChannelPrototype>(channelId, out var channelProto))
        {
            var message = Loc.GetString("handheld-radio-component-chennel-examine",
                ("channel", channelProto.LocalizedName));
            _popup.PopupEntity(message, user, user);
        }
    }

    private void UpdateChannelOnComponents(EntityUid uid, string channelId)
    {
        // Обновляем RadioMicrophoneComponent
        if (TryComp<RadioMicrophoneComponent>(uid, out var mic))
        {
            mic.BroadcastChannel = channelId;
        }

        // Обновляем RadioSpeakerComponent
        if (TryComp<RadioSpeakerComponent>(uid, out var speaker))
        {
            speaker.Channels = new HashSet<string> { channelId };
            UpdateSpeakerActiveChannels(uid, speaker);
        }

        // Обновляем StationRadioReceiverComponent (через систему трансляций)
        if (TryComp<StationRadioReceiverComponent>(uid, out var receiver))
        {
            // Используем систему трансляций для правильного обновления
            _broadcastSystem.SubscribeToChannel(uid, channelId, receiver);

            // Отправляем событие для обновления состояния
            var ev = new StationRadioChannelChangedEvent(channelId);
            RaiseLocalEvent(uid, ev);
        }
    }

    private void OnChannelExamined(EntityUid uid, VerbSelectableRadioChannelComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!_protoMan.TryIndex<RadioChannelPrototype>(comp.SelectedChannelId, out var channelProto))
            return;

        using (args.PushGroup(nameof(VerbSelectableRadioChannelComponent)))
        {
            args.PushMarkup(Loc.GetString("handheld-radio-component-on-examine",
                ("frequency", channelProto.Frequency)));
            args.PushMarkup(Loc.GetString("handheld-radio-component-chennel-examine",
                ("channel", channelProto.LocalizedName)));
        }
    }
        // End ADT-Tweak
    #endregion
}