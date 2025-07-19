using Content.Shared._SD.Keypad;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._SD.Keypad.UI;

[UsedImplicitly]
public sealed class KeypadBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private KeypadMenu? _menu;

    public KeypadBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<KeypadMenu>();

        _menu.OnKeypadButtonPressed += i =>
        {
            SendMessage(new KeypadKeypadPressedMessage(i));
        };
        _menu.OnClearButtonPressed += () =>
        {
            SendMessage(new KeypadClearButtonPressedMessage());
        };
        _menu.OnEnterButtonPressed += () =>
        {
            SendMessage(new KeypadEnterButtonPressedMessage());
        };
        _menu.OnCancelButtonPressed += () =>
        {
            SendMessage(new KeypadCancelButtonPressedMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu != null && state is KeypadUISate cast)
            _menu.UpdateState(cast);
    }
}
