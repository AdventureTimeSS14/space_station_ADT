using Content.Client.Eui;
using Content.Shared.Cloning;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Content.Shared.ADT.Phantom;

namespace Content.Client.ADT.Phantom;

[UsedImplicitly]
public sealed class AcceptPhantomPowersEui : BaseEui
{
    private readonly AcceptPhantomPowersWindow _window;

    public AcceptPhantomPowersEui()
    {
        _window = new AcceptPhantomPowersWindow();

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new AcceptPhantomPowersChoiceMessage(AcceptPhantomPowersButton.Deny));
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new AcceptPhantomPowersChoiceMessage(AcceptPhantomPowersButton.Deny));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new AcceptPhantomPowersChoiceMessage(AcceptPhantomPowersButton.Accept));
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

