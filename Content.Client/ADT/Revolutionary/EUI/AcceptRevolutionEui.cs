using Content.Client.Eui;
using Content.Shared.Cloning;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Content.Shared.Revolutionary;
using Content.Shared.Bible.Components;

namespace Content.Client.Revolutionary;

[UsedImplicitly]
public sealed class AcceptRevolutionEui : BaseEui
{
    private readonly AcceptRevolutionWindow _window;

    public AcceptRevolutionEui()
    {
        _window = new AcceptRevolutionWindow();

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new AcceptRevolutionChoiceMessage(AcceptRevolutionButton.Deny));
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new AcceptRevolutionChoiceMessage(AcceptRevolutionButton.Deny));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new AcceptRevolutionChoiceMessage(AcceptRevolutionButton.Accept));
            _window.Close();
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

}
