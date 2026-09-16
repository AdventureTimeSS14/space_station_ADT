using Content.Shared.Inventory;

namespace Content.Shared.ADT.TTS;

public sealed class TransformSpeakerVoiceEvent(EntityUid sender, string voiceId) : EntityEventArgs, IInventoryRelayEvent
{
    public EntityUid Sender = sender;
    public string VoiceId = voiceId;

    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
}
