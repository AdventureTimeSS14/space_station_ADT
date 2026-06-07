using Content.Server.ADT.Language;
using Content.Server.ADT.TTS;
using Content.Server.Radio;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.TTS;
using Content.Shared.Corvax.TTS;
using Robust.Shared.Player;

namespace Content.Server.Corvax.TTS;

public sealed partial class TTSSystem
{
    private void OnHeadsetReceive(Entity<HeadsetTTSComponent> ent, ref RadioReceiveEvent args)
    {
        var parent = Transform(ent).ParentUid;

        if (!TryComp<TTSComponent>(args.RadioSource, out var tts) || tts.VoicePrototypeId == null)
            return;

        HandleRadio(parent, args.ChatMsg.Message.Message, tts.VoicePrototypeId, args.Language);
    }

    private async void HandleRadio(EntityUid uid, string message, string speaker, LanguagePrototype language)
    {
        if (language.LanguageType is not Generic gen)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var soundData = await GenerateTTS(message, speaker);

        if (soundData is null)
            return;

        var languageSoundData = await GenerateTTS(_language.ObfuscateMessage(uid, message, gen.Replacement, gen.ObfuscateSyllables, gen.ReplaceEntireMessage), speaker);

        if (languageSoundData is null)
            return;

        RaiseNetworkEvent(new PlayRadioTTSEvent(soundData, languageSoundData, language.ID), Filter.SinglePlayer(actor.PlayerSession));
    }
}
