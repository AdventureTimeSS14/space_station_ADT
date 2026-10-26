using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.UserInterface.Buttons;

public sealed class ADTUICommandButton : ADTCommandButton
{
    public Type? WindowType { get; set; }
    private DefaultWindow? _window;

    protected override void Execute(ButtonEventArgs obj)
    {
        if (WindowType == null)
            return;

        var windowInstance = IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance(WindowType);
        if (windowInstance is not DefaultWindow window)
            return;

        _window = window;
        _window.OpenCentered();
    }
}
