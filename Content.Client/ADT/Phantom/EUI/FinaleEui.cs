using Content.Client.Eui;
using Content.Shared.Cloning;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Content.Shared.ADT.Phantom;

namespace Content.Client.ADT.Phantom;

[UsedImplicitly]
public sealed class PhantomFinaleEui : BaseEui
{
    private readonly PhantomFinaleWindow _window;

    public PhantomFinaleEui()
    {
        _window = new PhantomFinaleWindow();

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new PhantomFinaleChoiceMessage(PhantomFinaleButton.Deny));
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new PhantomFinaleChoiceMessage(PhantomFinaleButton.Deny));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new PhantomFinaleChoiceMessage(PhantomFinaleButton.Accept));
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
