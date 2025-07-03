using Content.Shared.ADT.Morph;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Morph.UI;

public sealed partial class MimicryMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    [NonSerialized] private MimicryMenu? _menu;

    public MimicryMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_menu != null)
            {
                _menu.SendActivateMessageAction -= SendMessage;
            }
        }
        base.Dispose(disposing);
    }
    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<MimicryMenu>();
        _menu.SetEntity(Owner);
        _menu.SendActivateMessageAction += SendMessage;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / _displayManager.ScreenSize);
    }

    private void SendMessage(NetEntity netent)
    {
        base.SendMessage(new EventMimicryActivate() { Target = netent });
    }
}
