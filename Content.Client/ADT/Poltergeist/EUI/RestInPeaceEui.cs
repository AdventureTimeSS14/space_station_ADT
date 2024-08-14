using Content.Client.Eui;
using Content.Shared.Cloning;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Content.Shared.ADT.Poltergeist;

namespace Content.Client.ADT.Poltergeist;

[UsedImplicitly]
public sealed class RestInPeaceEui : BaseEui
{
    private readonly RestInPeaceWindow _window;

    public RestInPeaceEui()
    {
        _window = new RestInPeaceWindow();

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new RestInPeaceChoiceMessage(RestInPeaceButton.Deny));
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new RestInPeaceChoiceMessage(RestInPeaceButton.Deny));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new RestInPeaceChoiceMessage(RestInPeaceButton.Accept));
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

