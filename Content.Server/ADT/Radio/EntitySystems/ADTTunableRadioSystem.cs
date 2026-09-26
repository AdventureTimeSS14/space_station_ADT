using System.Linq;
using Content.Server.ADT.Language;
using Content.Server.ADT.TTS;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.Radio;
using Content.Shared.ADT.Radio.Components;
using Content.Shared.ADT.TTS;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Radio.EntitySystems;

public sealed class ADTTunableRadioSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly JammerSystem _jammer = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<(string Message, EntityUid Source, int Frequency)> _recentlySent = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTTunableRadioComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ADTTunableRadioComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ADTTunableRadioComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ADTTunableRadioComponent, ListenAttemptEvent>(OnListenAttempt);
        SubscribeLocalEvent<ADTTunableRadioComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

        SubscribeLocalEvent<ADTTunableRadioComponent, ADTTunableRadioSetFrequencyMessage>(OnSetFrequency);
        SubscribeLocalEvent<ADTTunableRadioComponent, ADTTunableRadioToggleMicrophoneMessage>(OnToggleMicrophone);
        SubscribeLocalEvent<ADTTunableRadioComponent, ADTTunableRadioToggleSpeakerMessage>(OnToggleSpeaker);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _recentlySent.Clear();
    }

    private void OnInit(Entity<ADTTunableRadioComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Frequency = Math.Clamp(ent.Comp.Frequency, ent.Comp.MinFrequency, ent.Comp.MaxFrequency);

        UpdateMicrophone(ent);
        UpdateVisuals(ent);
    }

    private void OnExamine(Entity<ADTTunableRadioComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(ADTTunableRadioComponent)))
        {
            args.PushMarkup(Loc.GetString("adt-tunable-radio-examine-frequency",
                ("frequency", ADTRadioFrequency.Format(ent.Comp.Frequency))));

            args.PushMarkup(Loc.GetString(ent.Comp.MicrophoneEnabled
                ? "adt-tunable-radio-examine-microphone-on"
                : "adt-tunable-radio-examine-microphone-off"));

            args.PushMarkup(Loc.GetString(ent.Comp.SpeakerEnabled
                ? "adt-tunable-radio-examine-speaker-on"
                : "adt-tunable-radio-examine-speaker-off"));
        }
    }

    private void OnGetVerbs(Entity<ADTTunableRadioComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        var microphone = ent.Comp.MicrophoneEnabled;
        var speaker = ent.Comp.SpeakerEnabled;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(microphone
                ? "adt-tunable-radio-verb-microphone-off"
                : "adt-tunable-radio-verb-microphone-on"),
            Act = () => SetMicrophoneEnabled(ent, !microphone, user),
        });

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(speaker
                ? "adt-tunable-radio-verb-speaker-off"
                : "adt-tunable-radio-verb-speaker-on"),
            Act = () => SetSpeakerEnabled(ent, !speaker, user),
        });
    }

    private void OnSetFrequency(Entity<ADTTunableRadioComponent> ent, ref ADTTunableRadioSetFrequencyMessage args)
    {
        SetFrequency(ent, args.Frequency, args.Actor);
    }

    private void OnToggleMicrophone(Entity<ADTTunableRadioComponent> ent, ref ADTTunableRadioToggleMicrophoneMessage args)
    {
        SetMicrophoneEnabled(ent, args.Enabled, args.Actor, quiet: true);
    }

    private void OnToggleSpeaker(Entity<ADTTunableRadioComponent> ent, ref ADTTunableRadioToggleSpeakerMessage args)
    {
        SetSpeakerEnabled(ent, args.Enabled, args.Actor, quiet: true);
    }

    public void SetFrequency(Entity<ADTTunableRadioComponent> ent, int frequency, EntityUid? user = null)
    {
        if (ent.Comp.Locked)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("adt-tunable-radio-locked"), user.Value, user.Value);

            return;
        }

        frequency = Math.Clamp(frequency, ent.Comp.MinFrequency, ent.Comp.MaxFrequency);
        if (frequency == ent.Comp.Frequency)
            return;

        ent.Comp.Frequency = frequency;
        Dirty(ent);

        _audio.PlayPvs(ent.Comp.SoundOnTune, ent);
    }

    public void SetMicrophoneEnabled(Entity<ADTTunableRadioComponent> ent, bool enabled, EntityUid? user = null, bool quiet = false)
    {
        if (ent.Comp.MicrophoneEnabled == enabled)
            return;

        ent.Comp.MicrophoneEnabled = enabled;
        Dirty(ent);

        UpdateMicrophone(ent);
        UpdateVisuals(ent);

        _audio.PlayPvs(ent.Comp.SoundOnToggle, ent);

        if (quiet || user == null)
            return;

        _popup.PopupEntity(Loc.GetString(enabled
            ? "adt-tunable-radio-popup-microphone-on"
            : "adt-tunable-radio-popup-microphone-off"), user.Value, user.Value);
    }

    public void SetSpeakerEnabled(Entity<ADTTunableRadioComponent> ent, bool enabled, EntityUid? user = null, bool quiet = false)
    {
        if (ent.Comp.SpeakerEnabled == enabled)
            return;

        ent.Comp.SpeakerEnabled = enabled;
        Dirty(ent);

        UpdateVisuals(ent);

        _audio.PlayPvs(ent.Comp.SoundOnToggle, ent);

        if (quiet || user == null)
            return;

        _popup.PopupEntity(Loc.GetString(enabled
            ? "adt-tunable-radio-popup-speaker-on"
            : "adt-tunable-radio-popup-speaker-off"), user.Value, user.Value);
    }

    private void UpdateMicrophone(Entity<ADTTunableRadioComponent> ent)
    {
        if (ent.Comp.MicrophoneEnabled)
            EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(ent);
    }

    private void UpdateVisuals(Entity<ADTTunableRadioComponent> ent)
    {
        _appearance.SetData(ent, RadioDeviceVisuals.Broadcasting, ent.Comp.MicrophoneEnabled);
        _appearance.SetData(ent, RadioDeviceVisuals.Speaker, ent.Comp.SpeakerEnabled);
    }

    private void OnListenAttempt(Entity<ADTTunableRadioComponent> ent, ref ListenAttemptEvent args)
    {
        if (HasComp<ADTTunableRadioComponent>(args.Source) || HasComp<RadioSpeakerComponent>(args.Source))
            args.Cancel();
    }

    private void OnListen(Entity<ADTTunableRadioComponent> ent, ref ListenEvent args)
    {
        if (!ent.Comp.MicrophoneEnabled)
            return;

        if (!_recentlySent.Add((args.Message, args.Source, ent.Comp.Frequency)))
            return;

        var language = args.Language ?? _language.GetCurrentLanguage(args.Source);
        Broadcast(ent, args.Source, args.Message, language);
    }

    public void Broadcast(Entity<ADTTunableRadioComponent> transmitter, EntityUid speaker, string message, LanguagePrototype language)
    {
        if (language.LanguageType is not Generic generic)
            return;

        if (_jammer.ShouldCancel(transmitter, transmitter.Comp.Frequency))
        {
            _audio.PlayPvs(transmitter.Comp.SoundOnReceive, transmitter);
            return;
        }

        var sourceMap = Transform(transmitter).MapID;
        var receivers = new List<Entity<ADTTunableRadioComponent>>();

        var query = EntityQueryEnumerator<ADTTunableRadioComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var radio, out var transform))
        {
            if (uid == transmitter.Owner)
                continue;

            if (!radio.SpeakerEnabled || radio.Frequency != transmitter.Comp.Frequency)
                continue;

            if (transform.MapID != sourceMap && !radio.CrossMap && !transmitter.Comp.CrossMap)
                continue;

            if (_jammer.ShouldCancel(uid, radio.Frequency))
                continue;

            receivers.Add((uid, radio));
        }

        if (receivers.Count == 0)
            return;

        var devices = new List<EntityUid>(receivers.Count);
        var understood = new HashSet<ICommonSession>();
        var foreign = new HashSet<ICommonSession>();

        CollectListeners(transmitter, language, understood, foreign);

        foreach (var receiver in receivers)
        {
            _audio.PlayPvs(receiver.Comp.SoundOnReceive, receiver);

            CollectListeners(receiver, language, understood, foreign);
            devices.Add(receiver.Owner);
        }

        SendChat(transmitter, speaker, message, generic, understood, foreign);

        var isWhisper = !receivers[0].Comp.Loud;

        var ttsEv = new ADTTunableRadioSpokeEvent(
            speaker,
            message,
            devices,
            language,
            transmitter.Comp.Effect,
            isWhisper);

        RaiseLocalEvent(ref ttsEv);
    }

    private void CollectListeners(
        Entity<ADTTunableRadioComponent> device,
        LanguagePrototype language,
        HashSet<ICommonSession> understood,
        HashSet<ICommonSession> foreign)
    {
        var range = device.Comp.Loud ? SharedChatSystem.VoiceRange : SharedChatSystem.WhisperMuffledRange;
        var devicePos = _transform.GetWorldPosition(device);

        foreach (var session in Filter.Pvs(device.Owner).Recipients)
        {
            if (session.AttachedEntity is not { } listener)
                continue;

            if ((devicePos - _transform.GetWorldPosition(listener)).Length() > range)
                continue;

            if (_language.CanUnderstand(listener, language))
            {
                understood.Add(session);
                foreign.Remove(session);
            }
            else if (!understood.Contains(session))
            {
                foreign.Add(session);
            }
        }
    }

    private void SendChat(
        Entity<ADTTunableRadioComponent> transmitter,
        EntityUid speaker,
        string message,
        Generic generic,
        HashSet<ICommonSession> understood,
        HashSet<ICommonSession> foreign)
    {
        if (understood.Count == 0 && foreign.Count == 0)
            return;

        var nameEv = new TransformSpeakerNameEvent(speaker, Name(speaker));
        RaiseLocalEvent(speaker, nameEv);

        var name = FormattedMessage.EscapeText(nameEv.VoiceName);
        var frequency = $"\\[{ADTRadioFrequency.Format(transmitter.Comp.Frequency)}\\]";

        message = ADTSpeechStress.Strip(message);

        if (understood.Count > 0)
        {
            var wrapped = WrapChat(transmitter, name, frequency, FormattedMessage.EscapeText(message), generic);
            _chatManager.ChatMessageToMany(
                ChatChannel.Radio,
                message,
                wrapped,
                EntityUid.Invalid,
                false,
                true,
                understood.Select(session => session.Channel));
        }

        if (foreign.Count == 0)
            return;

        var obfuscated = _language.ObfuscateMessage(
            speaker,
            FormattedMessage.EscapeText(message),
            generic.Replacement,
            generic.ObfuscateSyllables,
            generic.ReplaceEntireMessage);

        var wrappedForeign = WrapChat(transmitter, name, frequency, obfuscated, generic);
        _chatManager.ChatMessageToMany(
            ChatChannel.Radio,
            message,
            wrappedForeign,
            EntityUid.Invalid,
            false,
            false,
            foreign.Select(session => session.Channel));
    }

    private string WrapChat(Entity<ADTTunableRadioComponent> transmitter, string name, string frequency, string content, Generic generic)
    {
        if (generic.Color != null)
            content = $"[color={generic.Color.Value.ToHex()}]{content}[/color]";

        return Loc.GetString("adt-tunable-radio-chat-wrap",
            ("color", transmitter.Comp.ChatColor),
            ("frequency", frequency),
            ("name", name),
            ("message", content));
    }
}
