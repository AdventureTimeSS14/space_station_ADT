using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NewsReaderUiMessageEvent : CartridgeMessageEvent
{
    public readonly NewsReaderUiAction Action;

    // ADT-Tweak: комментарии к новостям
    /// <summary>
    ///     Comment content for the <see cref="NewsReaderUiAction.AddComment" /> action.
    /// </summary>
    public readonly string? Content;

    public NewsReaderUiMessageEvent(NewsReaderUiAction action, string? content = null)
    {
        Action = action;
        Content = content;
    }
}

[Serializable, NetSerializable]
public enum NewsReaderUiAction
{
    Next,
    Prev,
    NotificationSwitch,
    AddComment // ADT-Tweak
}
