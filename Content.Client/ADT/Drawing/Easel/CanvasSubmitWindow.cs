using System.Numerics;
using Content.Shared.ADT.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Drawing.Easel;

public sealed class CanvasSubmitWindow : DefaultWindow
{
    private readonly LineEdit _title;
    private readonly LineEdit _description;
    private readonly Button _submit;

    public CanvasSubmitWindow()
    {
        Title = Loc.GetString("adt-canvas-submit-title");
        MinSize = new Vector2(380, 200);
        SetSize = new Vector2(440, 220);

        _title = new LineEdit
        {
            PlaceHolder = Loc.GetString("adt-canvas-submit-name"),
            HorizontalExpand = true,
        };
        _title.IsValid = text => text.Length <= CanvasConstants.MaxTitleLength;
        _title.OnTextChanged += _ => UpdateButton();

        _description = new LineEdit
        {
            PlaceHolder = Loc.GetString("adt-canvas-submit-description"),
            HorizontalExpand = true,
        };
        _description.IsValid = text => text.Length <= CanvasConstants.MaxDescriptionLength;

        _submit = new Button
        {
            Text = Loc.GetString("adt-canvas-submit-send"),
            HorizontalExpand = true,
        };
        _submit.OnPressed += _ =>
        {
            Submitted?.Invoke(_title.Text.Trim(), _description.Text.Trim());
            Close();
        };

        var cancel = new Button
        {
            Text = Loc.GetString("adt-canvas-submit-cancel"),
            HorizontalExpand = true,
        };
        cancel.OnPressed += _ => Close();

        var buttons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };
        buttons.AddChild(_submit);
        buttons.AddChild(cancel);

        var hint = new Label
        {
            Text = Loc.GetString("adt-canvas-submit-hint"),
            FontColorOverride = Color.Gray,
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Margin = new Thickness(4),
        };
        root.AddChild(_title);
        root.AddChild(_description);
        root.AddChild(hint);
        root.AddChild(buttons);
        Contents.AddChild(root);

        UpdateButton();
    }

    public event Action<string, string>? Submitted;

    private void UpdateButton()
    {
        _submit.Disabled = _title.Text.Trim().Length == 0;
    }
}
