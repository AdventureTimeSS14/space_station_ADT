using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Client.Input;

namespace Content.Client.ADT.UI.Chat.Controls;

[Virtual]
public class ChatSearchBox : PanelContainer
{
    public const string StyleClassChatSearchBox = "ChatSearchBox";
    public const string StyleClassChatSearchLineEdit = "ChatSearchLineEdit";

    public event Action<string>? OnSearchChanged;
    public event Action? OnSearchClosed;

    public readonly LineEdit SearchInput;
    private readonly Button _closeButton;

    public string SearchText => SearchInput.Text;

    public ChatSearchBox()
    {
        AddStyleClass(StyleClassChatSearchBox);

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            HorizontalExpand = true,
            VerticalExpand = false
        };
        AddChild(container);

        var searchLabel = new Label
        {
            Text = Loc.GetString("hud-adt-chat-search-label"),
            VerticalAlignment = VAlignment.Center,
            StyleClasses = { "LabelSubText" }
        };
        container.AddChild(searchLabel);

        SearchInput = new LineEdit
        {
            Name = "SearchInput",
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("hud-adt-chat-search-placeholder"),
            StyleClasses = { StyleClassChatSearchLineEdit }
        };
        SearchInput.OnTextChanged += OnTextChanged;
        SearchInput.OnKeyBindDown += OnKeyBindDown;
        container.AddChild(SearchInput);

        _closeButton = new Button
        {
            Text = "✕",
            ToolTip = Loc.GetString("hud-adt-chat-search-close"),
            MinWidth = 28
        };
        _closeButton.OnPressed += _ => Close();
        container.AddChild(_closeButton);
    }

    private void OnTextChanged(LineEdit.LineEditEventArgs args)
    {
        OnSearchChanged?.Invoke(SearchInput.Text);
    }

    private void OnKeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.TextReleaseFocus)
        {
            Close();
            args.Handle();
        }
    }

    public void FocusSearch()
    {
        SearchInput.IgnoreNext = true;
        SearchInput.GrabKeyboardFocus();
        SearchInput.CursorPosition = SearchInput.Text.Length;
        SearchInput.SelectionStart = SearchInput.Text.Length;
    }

    public void Close()
    {
        SearchInput.ReleaseKeyboardFocus();
        OnSearchClosed?.Invoke();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        SearchInput.OnTextChanged -= OnTextChanged;
        SearchInput.OnKeyBindDown -= OnKeyBindDown;
    }
}

