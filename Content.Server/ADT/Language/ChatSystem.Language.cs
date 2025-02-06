using Robust.Shared.Utility;
using Content.Shared.ADT.Language;
using System.Text;
using Robust.Shared.Random;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    public (string, string) GetLanguageColoredMessages(EntityUid sender, string message, LanguagePrototype language)
    {
        string coloredMessage = _language.AccentuateMessage(sender, language.ID, message);
        string coloredLanguageMessage = _language.ObfuscateMessage(sender, message, language);
        if (TryComp<LanguageSpeakerComponent>(sender, out var languageSpeaker))
        {

        }

        if (language.Color != null)
        {
            coloredMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + coloredMessage + "[/color]";
            coloredLanguageMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + coloredLanguageMessage + "[/color]";
        }

        return (coloredMessage, coloredLanguageMessage);
    }

    public (string, string, string, string) GetColoredObfuscatedLanguageMessages(EntityUid source, string message, LanguagePrototype language)
    {
        var accentMessage = _language.AccentuateMessage(source, language.ID, message);
        var languageMessage = _language.ObfuscateMessage(source, message, language);
        var obfuscatedMessage = ObfuscateMessageReadability(accentMessage, 0.2f);
        var obfuscatedLanguageMessage = ObfuscateMessageReadability(languageMessage, 0.2f);

        if (language.Color != null)
        {
            accentMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + accentMessage + "[/color]";
            languageMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + languageMessage + "[/color]";
            obfuscatedMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + obfuscatedMessage + "[/color]";
            obfuscatedLanguageMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + obfuscatedLanguageMessage + "[/color]";
        }

        return (accentMessage, languageMessage, obfuscatedMessage, obfuscatedLanguageMessage);
    }
}
