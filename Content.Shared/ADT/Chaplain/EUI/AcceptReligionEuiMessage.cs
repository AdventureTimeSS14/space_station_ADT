using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Chaplain;

[Serializable, NetSerializable]
public enum AcceptReligionButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class AcceptReligionChoiceMessage : EuiMessageBase
{
    public readonly AcceptReligionButton Button;

    public AcceptReligionChoiceMessage(AcceptReligionButton button)
    {
        Button = button;
    }
}
