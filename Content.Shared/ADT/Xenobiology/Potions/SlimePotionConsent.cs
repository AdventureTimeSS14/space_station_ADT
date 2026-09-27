using Content.Shared.Eui;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Xenobiology.Potions;

[Serializable, NetSerializable]
public sealed class AcceptPotionChoiceMessage : EuiMessageBase
{
    public readonly AcceptPotionButton Button;

    public AcceptPotionChoiceMessage(AcceptPotionButton button)
    {
        Button = button;
    }
}

[Serializable, NetSerializable]
public enum AcceptPotionButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class SlimePotionConsentState : EuiStateBase
{
    public readonly string RequesterName;
    public readonly string NewName;
    public readonly Gender? NewGender;

    public SlimePotionConsentState(string requesterName, string newName, Gender? newGender = null)
    {
        RequesterName = requesterName;
        NewName = newName;
        NewGender = newGender;
    }
}