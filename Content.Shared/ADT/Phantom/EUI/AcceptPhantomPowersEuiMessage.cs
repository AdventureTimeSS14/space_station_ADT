using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Phantom;

[Serializable, NetSerializable]
public enum AcceptPhantomPowersButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class AcceptPhantomPowersChoiceMessage : EuiMessageBase
{
    public readonly AcceptPhantomPowersButton Button;

    public AcceptPhantomPowersChoiceMessage(AcceptPhantomPowersButton button)
    {
        Button = button;
    }
}
