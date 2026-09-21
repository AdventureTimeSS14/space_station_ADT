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
    private const char CustomEmoteSeparator = '=';

    private readonly List<(string Trigger, string Emote)> _customEmotes = new();

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

        _customEmotes.Clear();

        foreach (var line in newEmotes.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var sep = line.IndexOf(CustomEmoteSeparator);
            if (sep <= 0 || sep == line.Length - 1)
                continue;

            var trigger = line[..sep].Trim();
            var emote = line[(sep + 1)..].Trim();

            if (trigger.Length == 0 || emote.Length == 0)
                continue;

            _customEmotes.Add((trigger, emote));
        }

        CustomEmotesUpdated?.Invoke(newEmotes);
    }

    /// <summary>
    /// Если всё сообщение совпало с триггером, отправляет вместо него эмоут.
    /// </summary>
    private bool TrySendCustomEmote(ChatBox box, ChatSelectChannel channel, string text)
    {
        if (_customEmotes.Count == 0)
            return false;

        if (channel is not (ChatSelectChannel.Local or ChatSelectChannel.Whisper or ChatSelectChannel.Emotes))
            return false;

        var trimmed = text.Trim();

        foreach (var (trigger, emote) in _customEmotes)
        {
            if (!trimmed.Equals(trigger, StringComparison.CurrentCultureIgnoreCase))
                continue;

            if (emote.Length > MaxMessageLength)
            {
                box.AddLine(
                    Loc.GetString("chat-manager-max-message-length", ("maxMessageLength", MaxMessageLength)),
                    Color.Orange);
                return true;
            }

            _consoleHost.ExecuteCommand($"me \"{CommandParsing.Escape(emote)}\"");
            return true;
        }

        return false;
    }
}
