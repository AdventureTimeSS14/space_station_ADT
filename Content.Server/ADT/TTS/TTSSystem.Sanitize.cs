using System.Text.RegularExpressions;
using Content.Shared.Chat;

namespace Content.Server.ADT.TTS;

public sealed partial class TTSSystem
{
    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!_isEnabled)
            return;

        args.Message = args.Message.Replace("+", "");
    }

    private string Sanitize(string text)
    {
        text = text.Trim();
        text = UnsupportedCharsRegex().Replace(text, "");
        text = LatinCharRegex().Replace(text, ReplaceLat2Cyr);
        text = WordRegex().Replace(text, ReplaceMatchedWord);
        text = DecimalSeparatorRegex().Replace(text, " целых ");
        text = NumberRegex().Replace(text, ReplaceWord2Num);
        return text.Trim();
    }

    [GeneratedRegex(@"[^a-zA-Zа-яА-ЯёЁ0-9,\-+?!. ]")]
    private static partial Regex UnsupportedCharsRegex();

    [GeneratedRegex(@"[a-zA-Z]", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex LatinCharRegex();

    [GeneratedRegex(@"(?<![a-zA-Zа-яёА-ЯЁ])[a-zA-Zа-яёА-ЯЁ]+?(?![a-zA-Zа-яёА-ЯЁ])", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"(?<=[1-90])(\.|,)(?=[1-90])")]
    private static partial Regex DecimalSeparatorRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    private string ReplaceLat2Cyr(Match oneChar)
    {
        if (ReverseTranslit.TryGetValue(oneChar.Value.ToLower(), out var replace))
            return replace;
        return oneChar.Value;
    }

    private string ReplaceMatchedWord(Match word)
    {
        if (WordReplacement.TryGetValue(word.Value.ToLower(), out var replace))
            return replace;
        return word.Value;
    }

    private string ReplaceWord2Num(Match word)
    {
        if (!long.TryParse(word.Value, out var number))
            return word.Value;
        return NumberConverter.NumberToText(number);
    }

    private static readonly IReadOnlyDictionary<string, string> WordReplacement =
        new Dictionary<string, string>()
        {
            {"нт", "Эн Тэ"},
            {"смо", "Эс Мэ О"},
            {"гп", "Гэ Пэ"},
            {"рд", "Эр Дэ"},
            {"гсб", "Гэ Эс Бэ"},
            {"гв", "Гэ Вэ"},
            {"нр", "Эн Эр"},
            {"нра", "Эн Эра"},
            {"нру", "Эн Эру"},
            {"км", "Кэ Эм"},
            {"кма", "Кэ Эма"},
            {"кму", "Кэ Эму"},
            {"си", "Эс И"},
            {"срп", "Эс Эр Пэ"},
            {"цк", "Цэ Каа"},
            {"сцк", "Эс Цэ Каа"},
            {"пцк", "Пэ Цэ Каа"},
            {"оцк", "О Цэ Каа"},
            {"шцк", "Эш Цэ Каа"},
            {"ншцк", "Эн Эш Цэ Каа"},
            {"дсо", "Дэ Эс О"},
            {"рнд", "Эр Эн Дэ"},
            {"сб", "Эс Бэ"},
            {"рцд", "Эр Цэ Дэ"},
            {"брпд", "Бэ Эр Пэ Дэ"},
            {"рпд", "Эр Пэ Дэ"},
            {"рпед", "Эр Пед"},
            {"тсф", "Тэ Эс Эф"},
            {"срт", "Эс Эр Тэ"},
            {"обр", "О Бэ Эр"},
            {"кпк", "Кэ Пэ Каа"},
            {"пда", "Пэ Дэ А"},
            {"id", "Ай Ди"},
            {"мщ", "Эм Ще"},
            {"вт", "Вэ Тэ"},
            {"wt", "Вэ Тэ"},
            {"ерп", "Йе Эр Пэ"},
            {"се", "Эс Йе"},
            {"апц", "А Пэ Цэ"},
            {"лкп", "Эл Ка Пэ"},
            {"см", "Эс Эм"},
            {"ека", "Йе Ка"},
            {"ка", "Кэ А"},
            {"бса", "Бэ Эс Аа"},
            {"тк", "Тэ Ка"},
            {"бфл", "Бэ Эф Эл"},
            {"бщ", "Бэ Щэ"},
            {"кк", "Кэ Ка"},
            {"ск", "Эс Ка"},
            {"зк", "Зэ Ка"},
            {"ерт", "Йе Эр Тэ"},
            {"вкд", "Вэ Ка Дэ"},
            {"нтр", "Эн Тэ Эр"},
            {"пнт", "Пэ Эн Тэ"},
            {"авд", "А Вэ Дэ"},
            {"пнв", "Пэ Эн Вэ"},
            {"ссд", "Эс Эс Дэ"},
            {"крс", "Ка Эр Эс"},
            {"кпб", "Кэ Пэ Бэ"},
            {"сссп", "Эс Эс Эс Пэ"},
            {"крб", "Ка Эр Бэ"},
            {"бд", "Бэ Дэ"},
            {"сст", "Эс Эс Тэ"},
            {"скс", "Эс Ка Эс"},
            {"икн", "И Ка Эн"},
            {"нсс", "Эн Эс Эс"},
            {"емп", "Йе Эм Пэ"},
            {"бс", "Бэ Эс"},
            {"цкс", "Цэ Ка Эс"},
            {"срд", "Эс Эр Дэ"},
            {"жпс", "Джи Пи Эс"},
            {"gps", "Джи Пи Эс"},
            {"ннксс", "Эн Эн Ка Эс Эс"},
            {"ss", "Эс Эс"},
            {"тесла", "тэсла"},
            {"трейзен", "трэйзэн"},
            {"нанотрейзен", "нанотрэйзэн"},
            {"рпзд", "Эр Пэ Зэ Дэ"},
            {"кз", "Кэ Зэ"},
            {"рхбз", "Эр Хэ Бэ Зэ"},
            {"рхбзз", "Эр Хэ Бэ Зэ Зэ"},
            {"днк", "Дэ Эн Ка"},
            {"мк", "Эм Ка"},
            {"mk", "Эм Ка"},
            {"рпг", "Эр Пэ Гэ"},
            {"с4", "Си 4"}, // cyrillic
            {"c4", "Си 4"}, // latinic
            {"бсс", "Бэ Эс Эс"},
            {"сии", "Эс И И"},
            {"ии", "И И"},
            {"опз", "О Пэ Зэ"},
            {"рпс", "Эр Пэ Эс"},
            {"рсу", "Эр Сэ У"},
            {"осщ", "О Сэ Ще"},
            {"ррт", "Эр Эр Тэ"},
            {"cqc", "Си Кью Си"},
            {"бармен", "бармэн"},
            {"бармена", "бармэна"},
            {"бармену", "бармэну"},
            {"барменом", "бармэном"},
            {"бармене", "бармэне"},
            {"бармены", "бармэны"},
            {"барменов", "бармэнов"},
            {"барменам", "бармэнам"},
            {"барменами", "бармэнами"},
            {"барменах", "бармэнах"},
        };

    private static readonly IReadOnlyDictionary<string, string> ReverseTranslit =
        new Dictionary<string, string>()
        {
            {"a", "а"},
            {"b", "б"},
            {"v", "в"},
            {"g", "г"},
            {"d", "д"},
            {"e", "е"},
            {"je", "ё"},
            {"zh", "ж"},
            {"z", "з"},
            {"i", "и"},
            {"y", "й"},
            {"k", "к"},
            {"l", "л"},
            {"m", "м"},
            {"n", "н"},
            {"o", "о"},
            {"p", "п"},
            {"r", "р"},
            {"s", "с"},
            {"t", "т"},
            {"u", "у"},
            {"f", "ф"},
            {"h", "х"},
            {"c", "ц"},
            {"x", "кс"},
            {"ch", "ч"},
            {"sh", "ш"},
            {"jsh", "щ"},
            {"hh", "ъ"},
            {"ih", "ы"},
            {"jh", "ь"},
            {"eh", "э"},
            {"ju", "ю"},
            {"ja", "я"},
        };
}
