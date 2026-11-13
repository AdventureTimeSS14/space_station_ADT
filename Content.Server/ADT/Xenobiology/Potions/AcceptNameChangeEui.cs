using Content.Server.EUI;
using Content.Shared.ADT.Xenobiology.Potions;
using Content.Shared.Eui;

namespace Content.Server.ADT.Xenobiology.Potions;

public sealed class AcceptNameChangeEui : BaseEui
{
    private readonly EntityUid _target;
    private readonly EntityUid _targetMind;
    private readonly EntityUid _potion;
    private readonly string _newName;
    private readonly string _requesterName;
    private readonly SlimePotionConsentSystem _system;

    public AcceptNameChangeEui(EntityUid target, EntityUid targetMind, EntityUid potion, string newName,
        string requesterName, SlimePotionConsentSystem system)
    {
        _target = target;
        _targetMind = targetMind;
        _potion = potion;
        _newName = newName;
        _requesterName = requesterName;
        _system = system;
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new SlimePotionConsentState(_requesterName, _newName);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptPotionChoiceMessage choice || choice.Button == AcceptPotionButton.Deny)
        {
            Close();
            return;
        }

        if (!_system.ValidateTargetMind(_target, _targetMind))
        {
            Close();
            return;
        }

        _system.DoRename(_target, _newName, _potion);
        Close();
    }
}