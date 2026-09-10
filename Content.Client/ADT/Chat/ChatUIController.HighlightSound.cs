using System.Text.RegularExpressions;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.UI.Chat;
using Content.Shared.Chat;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    private void TryPlayHighlightSound(ChatMessage msg, bool playHighlightSound)
    {
        if (!playHighlightSound)
            return;

        if (!_config.GetCVar(ADTCCVars.ChatHighlightSoundEnabled))
            return;

        var sound = _config.GetCVar(ADTCCVars.ChatHighlightSoundPath);
        if (string.IsNullOrEmpty(sound))
            return;

        if (!IsHighlightMatched(msg))
            return;

        var gain = _config.GetCVar(ADTCCVars.ChatHighlightSoundVolume);
        var audioParams = AudioParams.Default.WithVolume(SharedAudioSystem.GainToVolume(gain));

        _ent.System<AudioSystem>().PlayGlobal(new SoundPathSpecifier(sound), Filter.Local(), false, audioParams);
    }

    private bool IsHighlightMatched(ChatMessage msg)
    {
        if (_highlights.Count != 0)
        {
            var searchText = GetMessageAfterNameTag(msg.WrappedMessage);

            foreach (var highlight in _highlights)
            {
#pragma warning disable RA0026
                if (Regex.IsMatch(searchText, "(?i)(" + highlight + ")(?-i)(?![^[]*])"))
                    return true;
#pragma warning restore RA0026
            }
        }

        if (msg.Channel is not (ChatChannel.Radio or ChatChannel.Local or ChatChannel.Whisper)
            || _player.LocalEntity == null
            || !_ent.TryGetComponent<HighlightWordsInChatComponent>(_player.LocalEntity.Value, out var hlWords))
            return false;

        foreach (var (_, locStrings) in hlWords.HighlightWords)
        {
            foreach (var locString in locStrings)
            {
                if (SharedChatSystem.MessageTextContains(msg, Loc.GetString(locString)))
                    return true;
            }
        }

        return false;
    }

    private static string GetMessageAfterNameTag(string wrappedMessage)
    {
        var nameStart = wrappedMessage.IndexOf("[Name]", StringComparison.Ordinal);
        var nameEnd = wrappedMessage.IndexOf("[/Name]", StringComparison.Ordinal);

        if (nameStart < 0 || nameEnd <= nameStart)
            return wrappedMessage;

        return wrappedMessage[(nameEnd + "[/Name]".Length)..];
    }
}