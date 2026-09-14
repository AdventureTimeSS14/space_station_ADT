using Content.Server.EUI;
using Content.Shared.ADT.Xenobiology.Potions;
using Content.Shared.Eui;
using Robust.Shared.Enums;

namespace Content.Server.ADT.Xenobiology.Potions;

public sealed class AcceptGenderChangeEui : BaseEui
{
    private readonly EntityUid _target;
    private readonly EntityUid _potion;
    private readonly Gender _gender;
    private readonly string _requesterName;
    private readonly SlimePotionConsentSystem _system;

    public AcceptGenderChangeEui(EntityUid target, EntityUid potion, Gender gender,
        string requesterName, SlimePotionConsentSystem system)
    {
        _target = target;
        _potion = potion;
        _gender = gender;
        _requesterName = requesterName;
        _system = system;
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new SlimePotionConsentState(_requesterName, string.Empty, _gender);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptPotionChoiceMessage choice || choice.Button == AcceptPotionButton.Deny)
        {
            Close();
            return;
        }

        _system.DoGenderChange(_target, _gender, _potion);
        Close();
    }
}