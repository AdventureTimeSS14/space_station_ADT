using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Phantom;

[Serializable, NetSerializable]
public enum PhantomFinaleButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class PhantomFinaleChoiceMessage : EuiMessageBase
{
    public readonly PhantomFinaleButton Button;

    public PhantomFinaleChoiceMessage(PhantomFinaleButton button)
    {
        Button = button;
    }
}
