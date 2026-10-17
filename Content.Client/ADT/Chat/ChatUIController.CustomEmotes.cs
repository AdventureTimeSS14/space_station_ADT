using System.Text.RegularExpressions;
using Content.Client.ADT.Chat;
using Content.Client.ADT.Chat.UI;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.Chat;
using Robust.Shared.ContentPack;
using Robust.Shared.Console;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// Пользовательские замены текста на эмоуты. Хранятся только у клиента,
/// сервер получает обычную команду "me".
/// </summary>
public sealed partial class ChatUIController
{
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private List<(Regex Regex, string Emote)> _customEmotes = new();

    private string _customEmotesRaw = string.Empty;

    private CustomEmotesWindow? _customEmotesWindow;

    private void InitializeCustomEmotes()
    {
        _customEmotesRaw = CustomEmoteStorage.Load(_resources, _serialization, _sawmill);
        _customEmotes = CustomEmoteParser.Parse(_customEmotesRaw);
    }

    public void UpdateCustomEmotes(string newEmotes)
    {
        _customEmotesRaw = newEmotes;
        CustomEmoteStorage.Save(_resources, _serialization, _sawmill, newEmotes);

        _customEmotes = CustomEmoteParser.Parse(newEmotes);
    }

    public void OpenCustomEmotesWindow()
    {
        if (_customEmotesWindow is { Disposed: false, IsOpen: true })
        {
            _customEmotesWindow.MoveToFront();
            return;
        }

        if (_customEmotesWindow is null or { Disposed: true })
        {
            _customEmotesWindow = new CustomEmotesWindow();
            _customEmotesWindow.OnApply += UpdateCustomEmotes;
        }

        _customEmotesWindow.SetEntries(_customEmotesRaw);
        _customEmotesWindow.OpenCentered();
    }

    private bool TrySendCustomEmote(ChatBox box, ChatSelectChannel channel, string text)
    {
        if (_customEmotes.Count == 0)
            return false;

        if (channel is not (ChatSelectChannel.Local or ChatSelectChannel.Whisper or ChatSelectChannel.Emotes))
            return false;

        if (!CustomEmoteParser.TryApply(_customEmotes, text, out var cleaned, out var emote))
            return false;

        if (emote!.Length > MaxMessageLength)
        {
            box.AddLine(
                Loc.GetString("chat-manager-max-message-length", ("maxMessageLength", MaxMessageLength)),
                Color.Orange);
            return true;
        }

        _consoleHost.ExecuteCommand($"me \"{CommandParsing.Escape(emote)}\"");

        if (cleaned.Length > 0)
            _manager.SendMessage(cleaned, channel);

        return true;
    }
}
