using Content.Shared.Chat;
using Content.Server.Speech.Components;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.Corvax.TTS;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class VoiceOverrideSystem
{
    private void InitializeTTS()
    {
        SubscribeLocalEvent<VoiceOverrideComponent, TransformSpeakerVoiceEvent>(OnTransformSpeakerVoice);
    }

    private void OnTransformSpeakerVoice(Entity<VoiceOverrideComponent> entity, ref TransformSpeakerVoiceEvent args)
    {
        if (!entity.Comp.Enabled)
            return;

        args.VoiceId = entity.Comp.TTS ?? args.VoiceId;
    }
}
