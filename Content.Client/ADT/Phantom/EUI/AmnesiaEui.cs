using Content.Client.Eui;
using Content.Shared.Cloning;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Content.Shared.ADT.Phantom;

namespace Content.Client.ADT.Phantom;

[UsedImplicitly]
public sealed class PhantomAmnesiaEui : BaseEui
{
    private readonly PhantomAmnesiaWindow _window;

    public PhantomAmnesiaEui()
    {
        _window = new PhantomAmnesiaWindow();

        _window.OnClose += () => SendMessage(new PhantomAmnesiaChoiceMessage(PhantomAmnesiaButton.Accept));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new PhantomAmnesiaChoiceMessage(PhantomAmnesiaButton.Accept));
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
