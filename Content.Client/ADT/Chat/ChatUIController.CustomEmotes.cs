using System.Text.RegularExpressions;
using Content.Client.ADT.Chat;
using Content.Shared.ADT.CCVar;
using Content.Shared.Chat;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// Пользовательские замены текста на эмоуты. Хранятся только у клиента,
/// сервер получает обычную команду "me".
/// </summary>
public sealed partial class ChatUIController
{
    private List<(Regex Regex, string Emote)> _customEmotes = new();

    public event Action<string>? CustomEmotesUpdated;

    private void InitializeCustomEmotes()
    {
        var saved = _config.GetCVar(ADTCCVars.ChatCustomEmotes);

        if (!string.IsNullOrEmpty(saved))
            UpdateCustomEmotes(saved, true);
    }

    public void UpdateCustomEmotes(string newEmotes, bool firstLoad = false)
    {
        if (!firstLoad)
        {
            _config.SetCVar(ADTCCVars.ChatCustomEmotes, newEmotes);
            _config.SaveToFile();
        }

        _customEmotes = CustomEmoteParser.Parse(newEmotes);

        CustomEmotesUpdated?.Invoke(newEmotes);
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
