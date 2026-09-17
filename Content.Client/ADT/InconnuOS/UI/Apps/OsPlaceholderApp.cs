using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class OsPlaceholderApp : OsAppControl
{
    private readonly Label _label;

    public OsPlaceholderApp()
    {
        _label = new Label
        {
            Modulate = OsStyle.TextDim,
            Align = Label.AlignMode.Center,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };

        AddChild(_label);
    }

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        var name = Loc.GetString(Proto.Name);

        _label.Text = argument == null
            ? Loc.GetString("os-app-placeholder", ("app", name))
            : Loc.GetString("os-app-placeholder-file", ("app", name), ("file", argument));
    }
}
