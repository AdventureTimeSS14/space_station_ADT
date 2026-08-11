using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.ADT.UI.Chat;
public sealed class ChatSearchController : UIController
{
    public string SearchQuery { get; private set; } = string.Empty;

    public bool IsActive => !string.IsNullOrWhiteSpace(SearchQuery);
    public void SetSearch(ChatBox chatBox, string query)
    {
        SearchQuery = query;
        chatBox.SetSearchFilter(query);
    }
    public void ClearSearch(ChatBox chatBox)
    {
        SearchQuery = string.Empty;
        chatBox.SetSearchFilter(string.Empty);
    }
    public bool MatchesQuery(ChatMessage message, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        return message.Message.Contains(query, StringComparison.CurrentCultureIgnoreCase)
               || message.WrappedMessage.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }
}

public static class ChatSearchExtensions
{
    public static bool ContainsIgnoreCase(this string haystack, string needle)
    {
        return haystack.Contains(needle, StringComparison.CurrentCultureIgnoreCase);
    }
}

