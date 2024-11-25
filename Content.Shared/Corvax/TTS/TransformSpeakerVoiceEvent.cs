using Content.Shared.Inventory;

namespace Content.Shared.Corvax.TTS;

public sealed class TransformSpeakerVoiceEvent(EntityUid sender, string voiceId) : EntityEventArgs, IInventoryRelayEvent    // ADT voicemask fix
{
    public EntityUid Sender = sender;
    public string VoiceId = voiceId;

    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;   // ADT voicemask fix
}
