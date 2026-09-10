using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.Language;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.TTS;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Content.Shared.Radio;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.TTS;

public sealed partial class TTSSystem
{
    private static readonly ProtoId<RadioChannelPrototype> HandheldChannel = "Handheld";

    private readonly record struct RadioSpeakerListener(
        ICommonSession Session,
        NetEntity Device,
        TTSVariant Variant,
        float Distance);

    private void InitializeTunableRadio()
    {
        SubscribeLocalEvent<ADTTunableRadioSpokeEvent>(OnTunableRadioSpoke);
    }

    private void OnTunableRadioSpoke(ref ADTTunableRadioSpokeEvent args)
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

        var range = args.IsWhisper ? ChatSystem.WhisperMuffledRange : ChatSystem.VoiceRange;

        var listeners = new Dictionary<ICommonSession, RadioSpeakerListener>();
        foreach (var device in args.Speakers)
        {
            if (Deleted(device))
                continue;

            var devicePos = _xforms.GetWorldPosition(device);

            foreach (var session in Filter.Pvs(device).Recipients)
            {
                if (session.AttachedEntity is not { } listener)
                    continue;

                var distance = (devicePos - _xforms.GetWorldPosition(listener)).Length();
                if (distance > range)
                    continue;

                if (listeners.TryGetValue(session, out var known) && known.Distance <= distance)
                    continue;

                if (!HasComp<GhostHearingComponent>(listener) &&
                    !_examineSystem.InRangeUnOccluded(listener, device, range))
                    continue;

                if (_deafness.IsDeafened(listener))
                    continue;

                var variant = _language.CanUnderstand(listener, args.Language)
                    ? TTSVariant.Clear
                    : TTSVariant.Foreign;

                listeners[session] = new RadioSpeakerListener(session, GetNetEntity(device), variant, distance);
            }
        }

        if (listeners.Count == 0)
            return;

        var needed = new bool[VariantCount];
        foreach (var listener in listeners.Values)
        {
            needed[(int)listener.Variant] = true;
        }

        var texts = new string?[VariantCount];

        if (needed[(int)TTSVariant.Clear])
            texts[(int)TTSVariant.Clear] = args.Message;

        if (needed[(int)TTSVariant.Foreign])
            texts[(int)TTSVariant.Foreign] = Obfuscate(args.Source, args.Message, gen);

        SpeakFromRadios(protoVoice.Speaker, texts, needed, listeners.Values.ToList(), args.IsWhisper, args.Effect);
    }

    private async void SpeakFromRadios(
        string speaker,
        string?[] texts,
        bool[] needed,
        List<RadioSpeakerListener> listeners,
        bool isWhisper,
        string? effect)
    {
        var tasks = new Task<byte[]?>?[VariantCount];
        var pending = new List<Task<byte[]?>>();

        for (var i = 0; i < VariantCount; i++)
        {
            if (!needed[i] || texts[i] is not { } text)
                continue;

            var task = GenerateTTS(text, speaker, effect);
            tasks[i] = task;
            pending.Add(task);
        }

        if (pending.Count == 0)
            return;

        await Task.WhenAll(pending);

        var sounds = new byte[]?[VariantCount];

        for (var i = 0; i < VariantCount; i++)
        {
            if (tasks[i] is not { IsCompletedSuccessfully: true } task)
                continue;

            sounds[i] = await task;
        }

        foreach (var (session, device, variant, _) in listeners)
        {
            if (sounds[(int)variant] is not { } data)
                continue;

            if (session.Status != SessionStatus.InGame)
                continue;

            if (!TryGetEntity(device, out _))
                continue;

            RaiseNetworkEvent(new PlayTTSEvent(data, device, isWhisper, TTSKind.Radio, HandheldChannel), session);
        }
    }
}
