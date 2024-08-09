using Content.Client.Eui;
using Content.Shared.Cloning;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Content.Shared.ADT.Phantom;

namespace Content.Client.ADT.Phantom;

[UsedImplicitly]
public sealed class AcceptHelpingHandEui : BaseEui
{
    private readonly AcceptHelpingHandWindow _window;

    public AcceptHelpingHandEui()
    {
        _window = new AcceptHelpingHandWindow();

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new AcceptHelpingHandChoiceMessage(AcceptHelpingHandButton.Deny));
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new AcceptHelpingHandChoiceMessage(AcceptHelpingHandButton.Deny));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new AcceptHelpingHandChoiceMessage(AcceptHelpingHandButton.Accept));
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

