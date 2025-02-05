using Content.Shared.Inventory;

namespace Content.Shared.ADT.SpeechBarks;

public sealed class TransformSpeakerBarkEvent : EntityEventArgs, IInventoryRelayEvent
{
    public EntityUid Sender;
    public BarkData Data;

    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    public TransformSpeakerBarkEvent(EntityUid sender, BarkData data)
    {
        Sender = sender;
        Data = data;
    }
}
