using Content.Shared.Chat;
using Content.Server.Speech.Components;
using Content.Shared.ADT.SpeechBarks;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class VoiceOverrideSystem
{
    private void InitializeBarks()
    {
        SubscribeLocalEvent<VoiceOverrideComponent, TransformSpeakerBarkEvent>(OnTransformSpeakerBark);
    }

    private void OnTransformSpeakerBark(Entity<VoiceOverrideComponent> entity, ref TransformSpeakerBarkEvent args)
    {
        if (!entity.Comp.Enabled)
            return;

        args.Data = entity.Comp.Bark ?? args.Data;
    }
}
