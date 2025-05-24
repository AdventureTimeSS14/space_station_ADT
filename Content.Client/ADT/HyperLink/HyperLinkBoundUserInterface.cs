using Robust.Client.UserInterface;
using JetBrains.Annotations;
using Content.Shared.HyperLink;

namespace Content.Client.ADT.HyperLink;

[UsedImplicitly]
public sealed class HyperLinkBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HyperLinkWindow? _window;


    public HyperLinkBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HyperLinkWindow>();
        _window.ConfirmationButton.OnPressed += _ =>
        {
            SendMessage(new OpenURLMessage());
            Close();
        };
        _window.OpenCentered();
    }

}
