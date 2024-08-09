using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Phantom;

[Serializable, NetSerializable]
public enum PhantomAmnesiaButton
{
    Accept,
}

[Serializable, NetSerializable]
public sealed class PhantomAmnesiaChoiceMessage : EuiMessageBase
{
    public readonly PhantomAmnesiaButton Button;

    public PhantomAmnesiaChoiceMessage(PhantomAmnesiaButton button)
    {
        Button = button;
    }
}
