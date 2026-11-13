using Content.Shared.ADT.SpeechBarks;
using Content.Shared.ADT.TTS;
using Content.Shared.Speech.Components;

namespace Content.Server.ADT.Speech.EntitySystems;

public sealed class ADTVoiceOverrideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceOverrideComponent, TransformSpeakerVoiceEvent>(OnTransformSpeakerVoice);
        SubscribeLocalEvent<VoiceOverrideComponent, TransformSpeakerBarkEvent>(OnTransformSpeakerBark);
    }

    private void OnTransformSpeakerVoice(Entity<VoiceOverrideComponent> entity, ref TransformSpeakerVoiceEvent args)
    {
        if (!entity.Comp.Enabled)
            return;

        args.VoiceId = entity.Comp.TTS ?? args.VoiceId;
    }

    private void OnTransformSpeakerBark(Entity<VoiceOverrideComponent> entity, ref TransformSpeakerBarkEvent args)
    {
        if (!entity.Comp.Enabled)
            return;

        args.Data = entity.Comp.Bark ?? args.Data;
    }
}
