using System.Linq;
using System.Text;
using Content.Shared.ADT.Language;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Server.GameTicking.Events;
using Content.Server.Chat.Systems;

namespace Content.Server.ADT.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public int Seed { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LanguageSpeakerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);

        SubscribeNetworkEvent<LanguageChosenMessage>(OnLanguageSwitch);
    }

    private void OnMapInit(EntityUid uid, LanguageSpeakerComponent component, MapInitEvent args)
    {
        if (component.CurrentLanguage == null)
            component.CurrentLanguage = component.Languages.Keys.Where(x => (int)component.Languages[x] > 0).FirstOrDefault("Universal");
        UpdateUi(uid);
    }

    private void OnRoundStart(RoundStartingEvent args)
    {
        Seed = _random.Next();
    }

    private void OnLanguageSwitch(LanguageChosenMessage args)
    {
        var uid = GetEntity(args.Uid);
        if (!TryComp<LanguageSpeakerComponent>(uid, out var component))
            return;
        if (!GetLanguagesKnowledged(uid, LanguageKnowledge.BadSpeak, out var langs, out _) || !langs.ContainsKey(args.SelectedLanguage))
            return;

        component.CurrentLanguage = args.SelectedLanguage;

        UpdateUi(uid);
    }

    public string ObfuscateMessage(EntityUid uid, string originalMessage, LanguagePrototype? proto = null)
    {
        if (proto == null)
        {
            proto = GetCurrentLanguage(uid);
        }

        var builder = new StringBuilder();
        if (proto.ObfuscateSyllables)
            ObfuscateSyllables(builder, originalMessage, proto);
        else
            ObfuscatePhrases(builder, originalMessage, proto);

        var result = builder.ToString();
        result = _chat.SanitizeInGameICMessage(uid, result, out _);

        return result;
    }

    public string ObfuscateMessage(EntityUid uid, string originalMessage, ProtoId<LanguagePrototype> protoId)
    {
        var proto = _proto.Index(protoId);
        var builder = new StringBuilder();
        if (proto.ObfuscateSyllables)
            ObfuscateSyllables(builder, originalMessage, proto);
        else
            ObfuscatePhrases(builder, originalMessage, proto);

        var result = builder.ToString();
        result = _chat.SanitizeInGameICMessage(uid, result, out _);

        return result;
    }

    // Message obfuscation and seed system taken from https://github.com/new-frontiers-14/frontier-station-14/pull/671
    private void ObfuscateSyllables(StringBuilder builder, string message, LanguagePrototype language)
    {
        // Go through each word. Calculate its hash sum and count the number of letters.
        // Replicate it with pseudo-random syllables of pseudo-random (but similar) length. Use the hash code as the seed.
        // This means that identical words will be obfuscated identically. Simple words like "hello" or "yes" in different langs can be memorized.
        var wordBeginIndex = 0;
        var hashCode = 0;
        for (var i = 0; i < message.Length; i++)
        {
            var ch = char.ToLower(message[i]);
            // A word ends when one of the following is found: a space, a sentence end, or EOM
            if (char.IsWhiteSpace(ch) || (ch is '.' or '!' or '?' or '~' or '-' or ',') || i == message.Length - 1)
            {
                var wordLength = i + 1 - wordBeginIndex;
                if (wordLength > 0)
                {
                    var newWordLength = PseudoRandomNumber(hashCode, 1, 4);

                    for (var j = 0; j < newWordLength; j++)
                    {
                        var index = PseudoRandomNumber(hashCode + j, 0, language.Replacement.Count);
                        builder.Append(language.Replacement[index]);
                    }
                }

                builder.Append(ch);
                hashCode = 0;
                wordBeginIndex = i + 1;
            }
            else
            {
                hashCode = hashCode * 31 + ch;
            }
        }
    }

    private void ObfuscatePhrases(StringBuilder builder, string message, LanguagePrototype language)
    {
        // In a similar manner, each phrase is obfuscated with a random number of conjoined obfuscation phrases.
        // However, the number of phrases depends on the number of characters in the original phrase.
        var sentenceBeginIndex = 0;
        for (var i = 0; i < message.Length; i++)
        {
            var ch = char.ToLower(message[i]);
            if ((ch is '.' or '!' or '?' or '~' or '-' or ',') || i == message.Length - 1)
            {
                var length = i + 1 - sentenceBeginIndex;
                if (length > 0)
                {
                    var newLength = (int) Math.Clamp(Math.Cbrt(length) - 1, 1, 4); // 27+ chars for 2 phrases, 64+ for 3, 125+ for 4.

                    for (var j = 0; j < newLength; j++)
                    {
                        var phrase = _random.Pick(language.Replacement);
                        builder.Append(phrase);
                    }
                }
                sentenceBeginIndex = i + 1;

                if ((ch is '.' or '!' or '?'))
                    builder.Append(ch).Append(" ");
            }
        }
    }

    private int PseudoRandomNumber(int seed, int min, int max)
    {
        // This is not a uniform distribution, but it shouldn't matter: given there's 2^31 possible random numbers,
        // The bias of this function should be so tiny it will never be noticed.
        seed += Seed;
        var random = ((seed * 1103515245) + 12345) & 0x7fffffff; // Source: http://cs.uccs.edu/~cs591/bufferOverflow/glibc-2.2.4/stdlib/random_r.c
        return random % (max - min) + min;
    }

    public string AccentuateMessage(EntityUid uid, string lang, string message)
    {
        if (!GetLanguagesKnowledged(uid, LanguageKnowledge.BadSpeak, out var langs, out _))
            return message;
        if (!langs.ContainsKey(lang))
            return message;
        if ((int)langs[lang] > (int)LanguageKnowledge.BadSpeak)
            return message;

        var sb = new StringBuilder();

        // This is pretty much ported from TG.
        foreach (var character in message)
        {
            if (_random.Prob(0.5f / 3f))
            {
                var lower = char.ToLowerInvariant(character);
                var newString = lower switch
                {
                    'o' => "u",
                    's' => "ch",
                    'a' => "ah",
                    'u' => "oo",
                    'c' => "k",
                    // Corvax-Localization Start
                    'о' => "а",
                    'к' => "кх",
                    'щ' => "шч",
                    'ц' => "тс",
                    // Corvax-Localization End
                    _ => $"{character}",
                };

                sb.Append(newString);
            }

            if (!_random.Prob(0.5f * 3/20))
            {
                sb.Append(character);
                continue;
            }

            var next = _random.Next(1, 3) switch
            {
                1 => "'",
                2 => $"{character}{character}",
                _ => $"{character}{character}{character}",
            };

            sb.Append(next);
        }

        return sb.ToString();
    }

    public override void UpdateUi(EntityUid uid, LanguageSpeakerComponent? comp = null)
    {
        base.UpdateUi(uid, comp);

        if (!Resolve(uid, ref comp))
            return;

        Dirty(uid, comp);

        if (!GetLanguages(uid, out var langs, out var translator, out var current))
            return;

        var state = new LanguageMenuStateMessage(GetNetEntity(uid), current, langs, translator);
        RaiseNetworkEvent(state, uid);
    }
}
