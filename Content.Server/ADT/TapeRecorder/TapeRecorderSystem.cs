using Content.Server.ADT.Language;
using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Speech;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.Chat;
using Content.Shared.Corvax.TTS;
using Content.Shared.Paper;
using Content.Shared.Speech;
using Content.Shared.TapeRecorder;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.TapeRecorder.Events;
using Robust.Shared.Prototypes;
using System.Text;

namespace Content.Server.TapeRecorder;

public sealed class TapeRecorderSystem : SharedTapeRecorderSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly LanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapeRecorderComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<TapeRecorderComponent, PrintTapeRecorderMessage>(OnPrintMessage);
    }

    /// <summary>
    /// Given a time range, play all messages on a tape within said range, [start, end).
    /// Split into this system as shared does not have ChatSystem access
    /// </summary>
    protected override void ReplayMessagesInSegment(Entity<TapeRecorderComponent> ent, TapeCassetteComponent tape, float segmentStart, float segmentEnd)
    {
        var speech = EnsureComp<SpeechComponent>(ent);

        foreach (var message in tape.RecordedData)
        {
            if (message.Timestamp < tape.CurrentPosition || message.Timestamp >= segmentEnd)
                continue;

            //Change the voice to match the speaker
            if (message.Bark != null)
            {
                var barkOverride = EnsureComp<SpeechBarksComponent>(ent);
                barkOverride.Data = message.Bark;
            }
            if (message.TTS.HasValue)
            {
                var ttsOverride = EnsureComp<TTSComponent>(ent);
                ttsOverride.VoicePrototypeId = message.TTS.Value;
            }

            // TODO: mimic the exact string chosen when the message was recorded
            var verb = message.Verb ?? SharedChatSystem.DefaultSpeechVerb;
            speech.SpeechVerb = _proto.Index(verb);

            var language = message.Language ?? _language.Universal;

            //Play the message
            _chat.TrySendInGameICMessage(ent, message.Message, InGameICChatType.Speak, false, nameOverride: message.Name ?? ent.Comp.DefaultName, language: _proto.Index(language));

            RemComp<SpeechBarksComponent>(ent);
            RemComp<TTSComponent>(ent);
        }
    }

    /// <summary>
    /// Whenever someone speaks within listening range, record it to tape
    /// </summary>
    private void OnListen(Entity<TapeRecorderComponent> ent, ref ListenEvent args)
    {
        // mode should never be set when it isn't active but whatever
        if (ent.Comp.Mode != TapeRecorderMode.Recording || !HasComp<ActiveTapeRecorderComponent>(ent))
            return;

        // No feedback loops
        if (args.Source == ent.Owner)
            return;

        if (!TryGetTapeCassette(ent, out var cassette))
            return;

        // TODO: Handle "Someone" when whispering from far away, needs chat refactor

        //Handle someone using a voice changer
        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);

        //Add a new entry to the tape
        var name = nameEv.VoiceName;
        var verb = _chat.GetSpeechVerb(args.Source, args.Message);

        BarkData? bark = null;
        ProtoId<TTSVoicePrototype>? tts = null;
        if (TryComp<SpeechBarksComponent>(args.Source, out var barksComponent))
        {
            var barkEv = new TransformSpeakerBarkEvent(args.Source, barksComponent.Data.Copy());
            RaiseLocalEvent(args.Source, barkEv);
            bark = barkEv.Data;
        }
        if (TryComp<TTSComponent>(args.Source, out var ttsComp) && ttsComp.VoicePrototypeId != null)
        {
            var ttsEv = new TransformSpeakerVoiceEvent(args.Source, ttsComp.VoicePrototypeId);
            RaiseLocalEvent(args.Source, ttsEv);
            tts = ttsEv.VoiceId;
        }

        cassette.Comp.Buffer.Add(new TapeCassetteRecordedMessage(cassette.Comp.CurrentPosition, name, verb, bark, tts, _language.GetCurrentLanguage(args.Source), args.Message));
    }

    private void OnPrintMessage(Entity<TapeRecorderComponent> ent, ref PrintTapeRecorderMessage args)
    {
        var (uid, comp) = ent;

        if (comp.CooldownEndTime > Timing.CurTime)
            return;

        if (!TryGetTapeCassette(ent, out var cassette))
            return;

        var text = new StringBuilder();
        var paper = Spawn(comp.PaperPrototype, Transform(ent).Coordinates);

        // Sorting list by time for overwrite order
        // TODO: why is this needed? why wouldn't it be stored in order
        var data = cassette.Comp.RecordedData;
        data.Sort((x,y) => x.Timestamp.CompareTo(y.Timestamp));

        // Looking if player's entity exists to give paper in its hand
        var player = args.Actor;
        if (Exists(player))
            _hands.PickupOrDrop(player, paper, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return;

        Audio.PlayPvs(comp.PrintSound, ent);

        text.AppendLine(Loc.GetString("tape-recorder-print-start-text"));
        text.AppendLine();
        foreach (var message in cassette.Comp.RecordedData)
        {
            var name = message.Name ?? ent.Comp.DefaultName;
            var time = TimeSpan.FromSeconds((double) message.Timestamp);

            var language = message.Language ?? _language.Universal;
            var languagedMessage = _language.CanUnderstand(uid, language) ? message.Message : _language.ObfuscateMessage(uid, message.Message, language);

            text.AppendLine(Loc.GetString("tape-recorder-print-message-text",
                ("time", time.ToString(@"hh\:mm\:ss")),
                ("source", name),
                ("message", languagedMessage)));
        }
        text.AppendLine();
        text.Append(Loc.GetString("tape-recorder-print-end-text"));

        _paper.SetContent((paper, paperComp), text.ToString());

        comp.CooldownEndTime = Timing.CurTime + comp.PrintCooldown;
    }
}
