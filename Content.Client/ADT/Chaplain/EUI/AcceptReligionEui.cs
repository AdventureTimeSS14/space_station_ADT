using Content.Client.Eui;
using Content.Shared.Cloning;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Content.Shared.Chaplain;
using Content.Shared.Bible.Components;

namespace Content.Client.Chaplain;

[UsedImplicitly]
public sealed class AcceptReligionEui : BaseEui
{
    private readonly AcceptReligionWindow _window;

    public AcceptReligionEui()
    {
        _window = new AcceptReligionWindow();

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new AcceptReligionChoiceMessage(AcceptReligionButton.Deny));
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new AcceptReligionChoiceMessage(AcceptReligionButton.Deny));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new AcceptReligionChoiceMessage(AcceptReligionButton.Accept));
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

