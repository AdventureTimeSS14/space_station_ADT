using Content.Shared.Inventory;

namespace Content.Shared.ADT.SpeechBarks;

public sealed class TransformSpeakerBarkEvent : EntityEventArgs, IInventoryRelayEvent
{
    public EntityUid Sender;
    public string Sound;
    public float Pitch;

    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    public TransformSpeakerBarkEvent(EntityUid sender, string sound, float pitch)
    {
        Sender = sender;
        Sound = sound;
        Pitch = pitch;
    }
}
