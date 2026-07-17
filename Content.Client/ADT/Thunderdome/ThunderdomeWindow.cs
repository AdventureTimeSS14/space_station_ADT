using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Graphics;
using System.Numerics;

namespace Content.Client.ADT.Thunderdome;

public abstract partial class ThunderdomeWindow : BaseWindow
{
    private const int BorderWidth = 2;
    private const int TitleBarHeight = 25;
    private const int DragMarginSize = 5;

    private readonly Label _titleLabel;
    private readonly TextureButton _closeButton;
    private readonly PanelContainer _bodyPanel;
    private readonly BoxContainer _contentBox;
    private readonly PanelContainer _outerBorder;
    private readonly PanelContainer _titleBar;

    public string WindowTitle
    {
        get => _titleLabel.Text ?? string.Empty;
        set => _titleLabel.Text = value;
    }

    public BoxContainer Contents => _contentBox;

    public ThunderdomeWindow()
    {
        MouseFilter = MouseFilterMode.Stop;
        Resizable = true;

        _outerBorder = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#1e1e24"),
                BorderColor = Color.FromHex("#5a5a5a"),
                BorderThickness = new Thickness(BorderWidth),
            },
        };
        AddChild(_outerBorder);

        var outerVBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };
        _outerBorder.AddChild(outerVBox);

        _titleBar = new PanelContainer
        {
            MinHeight = TitleBarHeight,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#2b2b36"),
                BorderColor = Color.FromHex("#ff0000"),
                BorderThickness = new Thickness(0, 0, 0, 1),
            },
        };

        var titleBarContent = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(10, 0),
        };

        var accentBar = new PanelContainer
        {
            MinWidth = 4,
            MaxWidth = 4,
            VerticalExpand = true,
            Margin = new Thickness(0, 6, 8, 6),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#ff0000"),
            },
        };
        titleBarContent.AddChild(accentBar);

        _titleLabel = new Label
        {
            Text = Loc.GetString("thunderdome-window-title"),
            StyleClasses = { "LabelHeadingBigger" },
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = true,
        };
        titleBarContent.AddChild(_titleLabel);

        _closeButton = new TextureButton
        {
            VerticalAlignment = VAlignment.Center,
            MinSize = new Vector2(24, 24),
        };

        var closeLabel = new Label
        {
            Text = "X",
            FontColorOverride = Color.FromHex("#ff0000"),
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
        };
        _closeButton.AddChild(closeLabel);

        _closeButton.OnPressed += _ => Close();
        _closeButton.OnMouseEntered += _ => closeLabel.FontColorOverride = Color.FromHex("#ff6666"); // Светло-красный при наведении
        _closeButton.OnMouseExited += _ => closeLabel.FontColorOverride = Color.FromHex("#ff0000");

        titleBarContent.AddChild(_closeButton);
        _titleBar.AddChild(titleBarContent);
        outerVBox.AddChild(_titleBar);

        _bodyPanel = new PanelContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        _contentBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        _bodyPanel.AddChild(_contentBox);
        outerVBox.AddChild(_bodyPanel);
    }

    protected override void Resized()
    {
        base.Resized();
        _outerBorder.SetSize = Size;
    }

    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        if (Resizable)
        {
            if (relativeMousePos.Y < DragMarginSize)
                return DragMode.Top;
            if (relativeMousePos.Y > Size.Y - DragMarginSize)
                return DragMode.Bottom;
            if (relativeMousePos.X < DragMarginSize)
                return DragMode.Left;
            if (relativeMousePos.X > Size.X - DragMarginSize)
                return DragMode.Right;
        }

        if (relativeMousePos.Y < TitleBarHeight + BorderWidth)
            return DragMode.Move;

        return DragMode.None;
    }
}