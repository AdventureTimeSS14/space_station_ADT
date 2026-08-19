using System.Text;
using Content.Server.ADT.Speech.Components;
using Content.Shared.ADT.Speech.EntitySystems;
using Content.Shared.Speech;
using Content.Shared.StatusEffect;

namespace Content.Server.ADT.Speech.EntitySystems;

public sealed class SyllableSystem : SharedSyllableSystem
{
    private const string Vowels = "аеёиоуыэюяАЕЁИОУЫЭЮЯ";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SyllableAccentComponent, AccentGetEvent>(OnAccent);
    }

    public override void DoSyllable(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        _statusEffectsSystem.TryAddStatusEffect<SyllableAccentComponent>(uid, SyllableKey, time, refresh, status);
    }

    private void OnAccent(EntityUid uid, SyllableAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }

    public string Accentuate(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var words = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return message;

        var finalMessage = new StringBuilder();

        for (var i = 0; i < words.Length; i++)
        {
            finalMessage.Append(Syllabify(words[i]));

            if (i < words.Length - 1 && HasMultipleSyllables(words[i]))
                finalMessage.Append("...");

            if (i < words.Length - 1)
                finalMessage.Append(' ');
        }

        return finalMessage.ToString();
    }

    private static string Syllabify(string word)
    {
        var result = new StringBuilder();
        var sawVowel = false;

        for (var i = 0; i < word.Length; i++)
        {
            var ch = word[i];
            var isVowel = Vowels.Contains(ch);

            // A syllable boundary goes before a consonant that is followed by a vowel
            // and comes after the first vowel of the word ("По-мо-ги-те").
            if (!isVowel && sawVowel && i + 1 < word.Length && Vowels.Contains(word[i + 1]))
                result.Append('-');

            result.Append(ch);

            if (isVowel)
                sawVowel = true;
        }

        return result.ToString();
    }

    private static bool HasMultipleSyllables(string word)
    {
        var count = 0;
        foreach (var ch in word)
        {
            if (Vowels.Contains(ch) && ++count > 1)
                return true;
        }

        return false;
    }
}
