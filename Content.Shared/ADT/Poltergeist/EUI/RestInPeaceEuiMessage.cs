using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Poltergeist;

[Serializable, NetSerializable]
public enum RestInPeaceButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class RestInPeaceChoiceMessage : EuiMessageBase
{
    public readonly RestInPeaceButton Button;

    public RestInPeaceChoiceMessage(RestInPeaceButton button)
    {
        Button = button;
    }
}
