using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Client.UserInterface.Systems.Chat
{
    public static class ChatTransliterationSystem
    {
        private static readonly IReadOnlyList<KeyValuePair<string, string>> RuToEn = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("а", "a"),
            new KeyValuePair<string, string>("б", "b"),
            new KeyValuePair<string, string>("в", "v"),
            new KeyValuePair<string, string>("г", "g"),
            new KeyValuePair<string, string>("д", "d"),
            new KeyValuePair<string, string>("е", "ye"),
            new KeyValuePair<string, string>("ё", "yo"),
            new KeyValuePair<string, string>("ж", "zh"),
            new KeyValuePair<string, string>("з", "z"),
            new KeyValuePair<string, string>("и", "i"),
            new KeyValuePair<string, string>("й", "y"),
            new KeyValuePair<string, string>("к", "k"),
            new KeyValuePair<string, string>("л", "l"),
            new KeyValuePair<string, string>("м", "m"),
            new KeyValuePair<string, string>("н", "n"),
            new KeyValuePair<string, string>("о", "o"),
            new KeyValuePair<string, string>("п", "p"),
            new KeyValuePair<string, string>("р", "r"),
            new KeyValuePair<string, string>("с", "s"),
            new KeyValuePair<string, string>("т", "t"),
            new KeyValuePair<string, string>("у", "u"),
            new KeyValuePair<string, string>("ф", "f"),
            new KeyValuePair<string, string>("х", "h"),
            new KeyValuePair<string, string>("ц", "c"),
            new KeyValuePair<string, string>("ч", "ch"),
            new KeyValuePair<string, string>("ш", "sh"),
            new KeyValuePair<string, string>("щ", "shch"),
            new KeyValuePair<string, string>("ъ", "''"),
            new KeyValuePair<string, string>("ы", "y"),
            new KeyValuePair<string, string>("ь", "'"),
            new KeyValuePair<string, string>("э", "e"),
            new KeyValuePair<string, string>("ю", "yu"),
            new KeyValuePair<string, string>("я", "ya")
        };
        private static readonly IReadOnlyList<KeyValuePair<string, string>> EnToRu = new List<KeyValuePair<string, string>>(){
            new KeyValuePair<string, string>("ye", "е"), //first in the foreach loop are the complex two letter transliterations
            new KeyValuePair<string, string>("yo", "ё"),
            new KeyValuePair<string, string>("zh", "ж"),
            new KeyValuePair<string, string>("''", "ъ"),
            new KeyValuePair<string, string>("shch", "щ"),
            new KeyValuePair<string, string>("ch", "ч"),
            new KeyValuePair<string, string>("sh", "ш"),
            new KeyValuePair<string, string>("yu", "ю"),
            new KeyValuePair<string, string>("ya", "я"),
            new KeyValuePair<string, string>("'", "ь"),
            new KeyValuePair<string, string>("a", "а"),
            new KeyValuePair<string, string>("b", "б"),
            new KeyValuePair<string, string>("c", "ц"),
            new KeyValuePair<string, string>("d", "д"),
            new KeyValuePair<string, string>("e", "е"),
            new KeyValuePair<string, string>("f", "ф"),
            new KeyValuePair<string, string>("g", "г"),
            new KeyValuePair<string, string>("h", "х"),
            new KeyValuePair<string, string>("i", "и"),
            new KeyValuePair<string, string>("j", "й"),
            new KeyValuePair<string, string>("k", "к"),
            new KeyValuePair<string, string>("l", "л"),
            new KeyValuePair<string, string>("m", "м"),
            new KeyValuePair<string, string>("n", "н"),
            new KeyValuePair<string, string>("o", "о"),
            new KeyValuePair<string, string>("q", "ку"),
            new KeyValuePair<string, string>("p", "п"),
            new KeyValuePair<string, string>("r", "р"),
            new KeyValuePair<string, string>("s", "с"),
            new KeyValuePair<string, string>("t", "т"),
            new KeyValuePair<string, string>("u", "у"),
            new KeyValuePair<string, string>("v", "в"),
            new KeyValuePair<string, string>("w", "в"),
            new KeyValuePair<string, string>("x", "кс"),
            new KeyValuePair<string, string>("y", "ы"),
            new KeyValuePair<string, string>("z", "з")
        };
        private static string Transliterate(string message, IReadOnlyList<KeyValuePair<string, string>> keysandvalues)
        {
            foreach (var (key, value) in keysandvalues)
            {
                var pattern = Regex.Escape(key);
                message = Regex.Replace(message, pattern, match =>
                {
                    var replacement = value;

                    if (!match.Value.Any(char.IsLower) && (match.Length > 1 || replacement.Length == 1))
                    {
                        replacement = replacement.ToUpperInvariant();
                    }
                    else if (match.Length >= 1 && replacement.Length >= 1 && char.IsUpper(match.Value[0]))
                    {
                        replacement = replacement[0].ToString().ToUpper() + replacement[1..];
                    }

                    return replacement;
                }, RegexOptions.IgnoreCase);
            }

            return message;
        }

        public static string TransliterateRussianToEnglish(string message)
        {
            return Transliterate(message, RuToEn);
        }

        public static string TransliterateEnglishToRussian(string message)
        {
            return Transliterate(message, EnToRu);
        }
    }
}
