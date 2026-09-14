using System.Numerics;
using Content.Shared.Chat;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

[Virtual]
public class ChatInputBox : PanelContainer
{
    public const string StyleClassChatPanel = "ChatPanel";
    public const string StyleClassChatLineEdit = "ChatLineEdit";
    public const string StyleClassChatFilterOptionButton = "ChatFilterOptionButton";
    public const string StyleClassChatSearchButton = "ChatSearchButton"; // ADT-Tweak

    public readonly ChannelSelectorButton ChannelSelector;
    public readonly HistoryLineEdit Input;
    public readonly ChannelFilterButton FilterButton;
    public readonly Button SearchButton; // ADT-Tweak
    protected readonly BoxContainer Container;
    protected ChatChannel ActiveChannel { get; private set; } = ChatChannel.Local;

    public event Action? OnSearchButtonPressed;// ADT-Tweak

    public ChatInputBox()
    {
        Container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4
        };
        AddChild(Container);

        ChannelSelector = new ChannelSelectorButton
        {
            Name = "ChannelSelector",
            ToggleMode = true,
            StyleClasses = { ChannelSelectorItemButton.StyleClassChatSelectorOptionButton },
            MinWidth = 75
        };
        Container.AddChild(ChannelSelector);
        Input = new HistoryLineEdit
        {
            Name = "Input",
            PlaceHolder = GetChatboxInfoPlaceholder(),
            HorizontalExpand = true,
            StyleClasses = { StyleClassChatLineEdit }
        };
        Container.AddChild(Input);

        // ADT-Tweak start
        SearchButton = new Button
        {
            Name = "SearchButton",
            Text = "🔍",
            ToolTip = Loc.GetString("hud-adt-chat-search-button-tooltip"),
            StyleClasses = { StyleClassChatFilterOptionButton },
            MinSize = new Vector2(28, 0)
        };
        SearchButton.OnPressed += _ => OnSearchButtonPressed?.Invoke();
        Container.AddChild(SearchButton);
        // ADT-Tweak end

        FilterButton = new ChannelFilterButton
        {
            Name = "FilterButton",
            StyleClasses = { StyleClassChatFilterOptionButton }
        };
        Container.AddChild(FilterButton);
        AddStyleClass(StyleClassChatPanel);
        ChannelSelector.OnChannelSelect += UpdateActiveChannel;
    }

    private void UpdateActiveChannel(ChatSelectChannel selectedChannel)
    {
        ActiveChannel = (ChatChannel) selectedChannel;
    }

    private static string GetChatboxInfoPlaceholder()
    {
        return (BoundKeyHelper.IsBound(ContentKeyFunctions.FocusChat),
                BoundKeyHelper.IsBound(ContentKeyFunctions.CycleChatChannelForward)) switch
            {
                (true, true) => Loc.GetString("hud-chatbox-info",
                    ("talk-key", BoundKeyHelper.ShortKeyName(ContentKeyFunctions.FocusChat)),
                    ("cycle-key", BoundKeyHelper.ShortKeyName(ContentKeyFunctions.CycleChatChannelForward))),
                (true, false) => Loc.GetString("hud-chatbox-info-talk",
                    ("talk-key", BoundKeyHelper.ShortKeyName(ContentKeyFunctions.FocusChat))),
                (false, true) => Loc.GetString("hud-chatbox-info-cycle",
                    ("cycle-key", BoundKeyHelper.ShortKeyName(ContentKeyFunctions.CycleChatChannelForward))),
                (false, false) => Loc.GetString("hud-chatbox-info-unbound")
            };
    }
}
