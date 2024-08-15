using Robust.Shared.Serialization;

namespace Content.Shared.VoiceMask;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeBarkMessage : BoundUserInterfaceMessage
{
    public string Proto { get; }

    public VoiceMaskChangeBarkMessage(string proto)
    {
        Proto = proto;
    }
}

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeBarkPitchMessage : BoundUserInterfaceMessage
{
    public string Pitch { get; }

    public VoiceMaskChangeBarkPitchMessage(string pitch)
    {
        Pitch = pitch;
    }
}
