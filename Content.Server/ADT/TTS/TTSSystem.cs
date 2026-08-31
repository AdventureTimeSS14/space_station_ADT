using System.Threading.Tasks;
using Content.Server.ADT.Deafness;
using Content.Server.ADT.Language;
using Content.Server.Chat.Systems;
using Content.Server.Examine;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.TTS;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.TTS;

public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly ADTDeafnessSystem _deafness = default!;
    [Dependency] private readonly ExamineSystem _examineSystem = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;

    /// <summary>
    /// What a specific listener should hear.
    /// </summary>
    private enum TTSVariant : byte
    {
        /// <summary>The language is understood and clearly audible.</summary>
        Clear = 0,

        /// <summary>The language is understood, but only fragments are audible.</summary>
        Muffled = 1,

        /// <summary>The language is not understood, but clearly audible.</summary>
        Foreign = 2,

        /// <summary>The language is not understood, and only fragments are audible.</summary>
        ForeignMuffled = 3,
    }

    private const int VariantCount = 4;

    private readonly record struct TTSListener(ICommonSession Session, TTSVariant Variant);

    private readonly List<string> _sampleText =
        new()
        {
            "Ох-х-хо-хо, ну и погодка.",
            "Едят ли кошки мошек, едят ли мошки кошек...",
            "Мне чизбургер с большой колой, пожалуйста.",
            "А ха ха ха, хорошая шутка.",
            "СИНГУЛЯРНОСТЬ СБЕЖАЛА!!",
            "Не хотите ли вы подписать мою петицию?",
            "Почему все вокруг путают нас с тобой?",
            "Не произноси это имя!!",
            "Иногда мне снится сыр...",
            "Почему... я не вижу потолок?",
            "Да прибудет с тобой сила.",
            "Нету ручек нет конфетки.",
            "Магнус не предавал!",
            "Так звучит мой голос.",
            "Здесь был котя... Или же не было?",
            "Здесь никого не было.",
            "Инконну был",
            "Съешь ещё этих мягких французских булок, да выпей же чаю",
        };

    private const int MaxMessageChars = 100 * 2;
    private bool _isEnabled;

    public override void Initialize()
    {
        _cfg.OnValueChanged(ADTTTSCVars.TTSEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        RegisterRateLimits();
        InitializeSanitize();
        InitializeRadio();
        InitializeTunableRadio();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(
            new PlayTTSEvent(soundData, kind: TTSKind.Preview),
            Filter.SinglePlayer(args.SenderSession));
    }

    private void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            component.VoicePrototypeId is not { } voiceId)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceEv.VoiceId, out var protoVoice))
            return;

        var effect = ResolveEffect(uid, component);

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, args.ObfuscatedMessage, protoVoice.Speaker, args.Language, effect);
            return;
        }

        HandleSay(uid, args.Message, protoVoice.Speaker, args.Language, effect);
    }

    private void HandleSay(EntityUid uid, string message, string speaker, LanguagePrototype language, string? effect)
    {
        if (language.LanguageType is not Generic gen)
            return;

        var needed = new bool[VariantCount];
        var listeners = new List<TTSListener>();

        foreach (var session in Filter.Pvs(uid).Recipients)
        {
            if (session.AttachedEntity is not { } listener)
                continue;

            if (!HasComp<GhostHearingComponent>(listener) &&
                !_examineSystem.InRangeUnOccluded(listener, uid, ChatSystem.VoiceRange))
                continue;

            if (listener != uid && _deafness.IsDeafened(listener))
                continue;

            var variant = _language.CanUnderstand(listener, language)
                ? TTSVariant.Clear
                : TTSVariant.Foreign;

            needed[(int)variant] = true;
            listeners.Add(new TTSListener(session, variant));
        }

        if (listeners.Count == 0)
            return;

        var texts = new string?[VariantCount];

        if (needed[(int)TTSVariant.Clear])
            texts[(int)TTSVariant.Clear] = message;

        if (needed[(int)TTSVariant.Foreign])
            texts[(int)TTSVariant.Foreign] = Obfuscate(uid, message, gen);

        Speak(uid, speaker, texts, needed, listeners, false, effect: effect);
    }

    private void HandleWhisper(EntityUid uid, string message, string obfMessage, string speaker, LanguagePrototype language, string? effect)
    {
        if (language.LanguageType is not Generic gen)
            return;

        // TODO: Check obstacles
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);

        var needed = new bool[VariantCount];
        var listeners = new List<TTSListener>();

        foreach (var session in Filter.Pvs(uid).Recipients)
        {
            if (session.AttachedEntity is not { } listener)
                continue;

            var xform = xformQuery.GetComponent(listener);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();
            if (distance > ChatSystem.VoiceRange)
                continue;

            if (!HasComp<GhostHearingComponent>(listener) &&
                !_examineSystem.InRangeUnOccluded(listener, uid, ChatSystem.WhisperMuffledRange))
                continue;

            bool muffled;
            if (listener != uid && _deafness.TryGetTTSHearing(listener, out var hearsMuffled))
            {
                if (!hearsMuffled)
                    continue;

                muffled = true;
            }
            else
            {
                muffled = distance > ChatSystem.WhisperClearRange;
            }

            var understands = _language.CanUnderstand(listener, language);
            var variant = (understands, muffled) switch
            {
                (true, false) => TTSVariant.Clear,
                (true, true) => TTSVariant.Muffled,
                (false, false) => TTSVariant.Foreign,
                (false, true) => TTSVariant.ForeignMuffled,
            };

            needed[(int)variant] = true;
            listeners.Add(new TTSListener(session, variant));
        }

        if (listeners.Count == 0)
            return;

        var texts = new string?[VariantCount];

        if (needed[(int)TTSVariant.Clear])
            texts[(int)TTSVariant.Clear] = message;

        if (needed[(int)TTSVariant.Muffled])
            texts[(int)TTSVariant.Muffled] = obfMessage;

        if (needed[(int)TTSVariant.Foreign])
            texts[(int)TTSVariant.Foreign] = Obfuscate(uid, message, gen);

        if (needed[(int)TTSVariant.ForeignMuffled])
            texts[(int)TTSVariant.ForeignMuffled] = Obfuscate(uid, obfMessage, gen);

        Speak(uid, speaker, texts, needed, listeners, true, effect: effect);
    }

    private async void Speak(
        EntityUid uid,
        string speaker,
        string?[] texts,
        bool[] needed,
        List<TTSListener> listeners,
        bool isWhisper,
        TTSKind kind = TTSKind.World,
        ProtoId<RadioChannelPrototype>? channel = null,
        string? effect = null)
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

        if (Deleted(uid))
            return;

        var source = kind == TTSKind.Radio ? null : (NetEntity?)GetNetEntity(uid);
        var events = new PlayTTSEvent?[VariantCount];

        for (var i = 0; i < VariantCount; i++)
        {
            if (tasks[i] is not { IsCompletedSuccessfully: true } task)
                continue;

            if (await task is { } data)
                events[i] = new PlayTTSEvent(data, source, isWhisper, kind, channel);
        }

        foreach (var (session, variant) in listeners)
        {
            if (events[(int)variant] is not { } ev)
                continue;

            if (session.Status != SessionStatus.InGame)
                continue;

            RaiseNetworkEvent(ev, session);
        }
    }

    private string Obfuscate(EntityUid uid, string message, Generic gen)
    {
        return _language.ObfuscateMessage(uid, message, gen.Replacement, gen.ObfuscateSyllables, gen.ReplaceEntireMessage);
    }

    private Task<byte[]?> GenerateTTS(string text, string speaker, string? effect = null)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "")
            return Task.FromResult<byte[]?>(null);

        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        return _ttsManager.ConvertTextToSpeech(speaker, textSanitized, effect);
    }
}
