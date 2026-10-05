using Content.Server.EUI;
using Content.Shared.ADT.Xenobiology.Potions;
using Content.Shared.Eui;

namespace Content.Server.ADT.Xenobiology.Potions;

public sealed class AcceptMindTransferenceEui : BaseEui
{
    private readonly EntityUid _user;
    private readonly EntityUid _target;
    private readonly EntityUid _targetMind;
    private readonly EntityUid _potion;
    private readonly string _requesterName;
    private readonly SlimePotionConsentSystem _system;

    public AcceptMindTransferenceEui(EntityUid user, EntityUid target, EntityUid targetMind, EntityUid potion,
        string requesterName, SlimePotionConsentSystem system)
    {
        _user = user;
        _target = target;
        _targetMind = targetMind;
        _potion = potion;
        _requesterName = requesterName;
        _system = system;
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new SlimePotionConsentState(_requesterName, string.Empty);
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

        _system.DoMindSwap(_user, _target, _potion);
        Close();
    }
}