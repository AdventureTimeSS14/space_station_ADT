using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Revolutionary;

[Serializable, NetSerializable]
public enum AcceptRevolutionButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class AcceptRevolutionChoiceMessage : EuiMessageBase
{
    public readonly AcceptRevolutionButton Button;

    public AcceptRevolutionChoiceMessage(AcceptRevolutionButton button)
    {
        Button = button;
    }
}
