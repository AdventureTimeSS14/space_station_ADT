using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chat;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Salvage.Systems;

[ByRefEvent]
public readonly record struct MegafaunaSpokeEvent(string Message);

public sealed partial class MegafaunaSystem
{
    public string? Say(EntityUid uid, string locId, int count = 1, bool force = false)
    {
        if (!TryComp<MegafaunaComponent>(uid, out var comp))
            return null;

        var now = _timing.CurTime;

        if (!force)
        {
            if (now < comp.NextSpeechTime)
                return null;

            if (!_random.Prob(comp.SpeechChance))
                return null;
        }

        comp.NextSpeechTime = now + comp.SpeechCooldown;

        var message = count > 1
            ? Loc.GetString($"{locId}-{_random.Next(1, count + 1)}")
            : Loc.GetString(locId);

        SpeakLine(uid, message, comp.SpeechFont, comp.SpeechFontSize);

        var ev = new MegafaunaSpokeEvent(message);
        RaiseLocalEvent(uid, ref ev);

        return message;
    }

    public void SpeakLine(EntityUid uid, string message, string font = "Blackcraft", int fontSize = 16)
    {
        if (!TryComp<MetaDataComponent>(uid, out var meta))
            return;

        var name = FormattedMessage.EscapeText(meta.EntityName);
        var wrappedMessage = Loc.GetString("chat-manager-entity-say-wrap-message",
            ("entityName", name),
            ("verb", Loc.GetString("chat-speech-verb-default")),
            ("fontType", font),
            ("fontSize", fontSize),
            ("defaultFont", "NotoSans"),
            ("defaultSize", 12),
            ("message", message));

        _chat.SendInVoiceRange(
            ChatChannel.Local,
            message,
            wrappedMessage,
            wrappedMessage,
            uid,
            ChatTransmitRange.HideChat);
    }
}
