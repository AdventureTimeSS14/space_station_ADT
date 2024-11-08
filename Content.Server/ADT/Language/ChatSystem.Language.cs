using Robust.Shared.Utility;
using Content.Shared.ADT.Language;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    public (string, string) GetLanguageColoredMessages(EntityUid sender, string message, LanguagePrototype language)
    {
        string coloredMessage = message;
        string coloredLanguageMessage = _language.ObfuscateMessage(sender, message, language);

        if (language.Color != null)
        {
            coloredMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + coloredMessage + "[/color]";
            coloredLanguageMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + coloredLanguageMessage + "[/color]";
        }

        return (coloredMessage, coloredLanguageMessage);
    }

    public (string, string) GetLanguageICSanitizedMessages(EntityUid sender, string message, LanguagePrototype language)
    {
        message = SanitizeInGameICMessage(sender, FormattedMessage.EscapeText(message), out _);
        string languageMessage = SanitizeInGameICMessage(sender, FormattedMessage.EscapeText(_language.ObfuscateMessage(sender, message, language)), out _);

        return (message, languageMessage);
    }

    public (string, string) GetObfuscatedLanguageMessages(EntityUid source, string message, LanguagePrototype language)
    {
        var obfuscatedMessage = ObfuscateMessageReadability(message, 0.2f);
        var obfuscatedLanguageMessage = ObfuscateMessageReadability(_language.ObfuscateMessage(source, message, language), 0.2f);

        return (obfuscatedMessage, obfuscatedLanguageMessage);
    }

    public (string, string, string, string) GetColoredObfuscatedLanguageMessages(EntityUid source, string message, LanguagePrototype language)
    {
        var languageMessage = _language.ObfuscateMessage(source, message, language);
        var obfuscatedMessage = ObfuscateMessageReadability(message, 0.2f);
        var obfuscatedLanguageMessage = ObfuscateMessageReadability(languageMessage, 0.2f);

        if (language.Color != null)
        {
            message = "[color=" + language.Color.Value.ToHex().ToString() + "]" + message + "[/color]";
            languageMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + languageMessage + "[/color]";
            obfuscatedMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + obfuscatedMessage + "[/color]";
            obfuscatedLanguageMessage = "[color=" + language.Color.Value.ToHex().ToString() + "]" + obfuscatedLanguageMessage + "[/color]";
        }

        return (message, languageMessage, obfuscatedMessage, obfuscatedLanguageMessage);
    }
}
