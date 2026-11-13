using Content.Client.Eui;
using Content.Shared.ADT.Xenobiology.Potions;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.ADT.Xenobiology.Potions;

[UsedImplicitly]
public sealed class AcceptNameChangeEui : BaseEui
{
    private readonly SlimePotionConsentWindow _window;
    private bool _choiceMade;

    public AcceptNameChangeEui()
    {
        _window = new SlimePotionConsentWindow("xeno-potion-name-window-title");
        WireUp(_window);
    }

    private void WireUp(SlimePotionConsentWindow window)
    {
        window.AcceptButton.OnPressed += _ =>
        {
            _choiceMade = true;
            SendMessage(new AcceptPotionChoiceMessage(AcceptPotionButton.Accept));
            window.Close();
        };

        window.DenyButton.OnPressed += _ =>
        {
            _choiceMade = true;
            SendMessage(new AcceptPotionChoiceMessage(AcceptPotionButton.Deny));
            window.Close();
        };

        window.OnClose += () =>
        {
            if (!_choiceMade)
                SendMessage(new AcceptPotionChoiceMessage(AcceptPotionButton.Deny));
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is SlimePotionConsentState consent)
            _window.SetPrompt(Loc.GetString("xeno-potion-name-window-prompt",
                ("user", consent.RequesterName),
                ("name", consent.NewName)));
    }

    public override void Closed()
    {
        _window.Close();
    }
}