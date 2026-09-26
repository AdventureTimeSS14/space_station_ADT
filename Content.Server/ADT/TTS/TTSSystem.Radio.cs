using Content.Server.ADT.Language;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.TTS;
using Content.Shared.Radio.Components;
using Robust.Shared.Player;

namespace Content.Server.ADT.TTS;

public sealed partial class TTSSystem
{
    private bool _radioEnabled;

    private void InitializeRadio()
    {
        _cfg.OnValueChanged(ADTTTSCVars.TTSRadioEnabled, v => _radioEnabled = v, true);

        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioSpoke);
    }

    private void OnRadioSpoke(ref RadioSpokeEvent args)
    {
        if (!_isEnabled || !_radioEnabled)
            return;

        if (args.Message.Length > MaxMessageChars)
            return;

        if (!TryComp<TTSComponent>(args.Source, out var tts) || tts.VoicePrototypeId is not { } voiceId)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(args.Source, voiceId);
        RaiseLocalEvent(args.Source, voiceEv);

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceEv.VoiceId, out var protoVoice))
            return;

        if (args.Language.LanguageType is not Generic gen)
            return;

        var needed = new bool[VariantCount];
        var listeners = new List<TTSListener>();

        foreach (var receiver in args.Receivers)
        {
            if (!TryGetHeadsetListener(receiver, out var session, out var listener))
                continue;

            if (listener == args.Source)
                continue;

            if (_deafness.IsDeafened(listener))
                continue;

            var variant = _language.CanUnderstand(listener, args.Language)
                ? TTSVariant.Clear
                : TTSVariant.Foreign;

            needed[(int)variant] = true;
            listeners.Add(new TTSListener(session, variant));
        }

        if (listeners.Count == 0)
            return;

        var texts = new string?[VariantCount];

        if (needed[(int)TTSVariant.Clear])
            texts[(int)TTSVariant.Clear] = args.Message;

        if (needed[(int)TTSVariant.Foreign])
            texts[(int)TTSVariant.Foreign] = Obfuscate(args.Source, args.Message, gen);

        Speak(
            args.Source,
            protoVoice.Speaker,
            texts,
            needed,
            listeners,
            isWhisper: false,
            kind: TTSKind.Radio,
            channel: args.Channel.ID,
            effect: GetChannelEffect(args.Channel.ID));
    }

    private bool TryGetHeadsetListener(EntityUid receiver, out ICommonSession session, out EntityUid listener)
    {
        session = default!;
        listener = default;

        if (!HasComp<HeadsetComponent>(receiver))
            return false;

        var wearer = Transform(receiver).ParentUid;
        if (!wearer.IsValid() || !TryComp<ActorComponent>(wearer, out var actor))
            return false;

        session = actor.PlayerSession;
        listener = wearer;
        return true;
    }

    private string GetChannelEffect(string channelId)
    {
        return _prototypeManager.TryIndex<TTSRadioChannelPrototype>(channelId, out var proto)
            ? proto.Effect
            : "radio_headset";
    }
}
