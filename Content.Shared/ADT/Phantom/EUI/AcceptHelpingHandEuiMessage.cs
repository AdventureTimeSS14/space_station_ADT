using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Phantom;

[Serializable, NetSerializable]
public enum AcceptHelpingHandButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class AcceptHelpingHandChoiceMessage : EuiMessageBase
{
    public readonly AcceptHelpingHandButton Button;

    public AcceptHelpingHandChoiceMessage(AcceptHelpingHandButton button)
    {
        Button = button;
    }
}
