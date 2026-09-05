using System.Text;

namespace Content.Shared.ADT.TTS.Sanitize;

// Source: https://codelab.ru/s/csharp/digits2phrase
public static class NumberConverter
{
    private static readonly string[] Frac20Male =
    {
        "", "один", "два", "три", "четыре", "пять", "шесть",
        "семь", "восемь", "девять", "десять", "одиннадцать",
        "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
        "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
    };

    private static readonly string[] Frac20Female =
    {
        "", "одна", "две", "три", "четыре", "пять", "шесть",
        "семь", "восемь", "девять", "десять", "одиннадцать",
        "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
        "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
    };

    private static readonly string[] Hunds =
    {
        "", "сто", "двести", "триста", "четыреста",
        "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот"
    };

    private static readonly string[] Tens =
    {
        "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят",
        "шестьдесят", "семьдесят", "восемьдесят", "девяносто"
    };

    public static string NumberToText(long value, bool male = true)
    {
        if (value >= (long)Math.Pow(10, 15))
            return String.Empty;

        if (value == 0)
            return "ноль";

        var str = new StringBuilder();

        if (value < 0)
        {
            str.Append("минус");
            value = -value;
        }

        value = AppendPeriod(value, 1000000000000, str, "триллион", "триллиона", "триллионов", true);
        value = AppendPeriod(value, 1000000000, str, "миллиард", "миллиарда", "миллиардов", true);
        value = AppendPeriod(value, 1000000, str, "миллион", "миллиона", "миллионов", true);
        value = AppendPeriod(value, 1000, str, "тысяча", "тысячи", "тысяч", false);

        var hundreds = (int)(value / 100);
        if (hundreds != 0)
            AppendWithSpace(str, Hunds[hundreds]);

        var less100 = (int)(value % 100);
        var frac20 = male ? Frac20Male : Frac20Female;
        if (less100 < 20)
            AppendWithSpace(str, frac20[less100]);
        else
        {
            var tens = less100 / 10;
            AppendWithSpace(str, Tens[tens]);
            var less10 = less100 % 10;
            if (less10 != 0)
                str.Append(" " + frac20[less100%10]);
        }

        return str.ToString();
    }

    private static void AppendWithSpace(StringBuilder stringBuilder, string str)
    {
        if (stringBuilder.Length > 0)
            stringBuilder.Append(" ");
        stringBuilder.Append(str);
    }

    private static long AppendPeriod(
        long value,
        long power,
        StringBuilder str,
        string declension1,
        string declension2,
        string declension5,
        bool male)
    {
        var thousands = (int)(value / power);
        if (thousands > 0)
        {
            AppendWithSpace(str, NumberToText(thousands, male, declension1, declension2, declension5));
            return value % power;
        }
        return value;
    }

    private static string NumberToText(
        long value,
        bool male,
        string valueDeclensionFor1,
        string valueDeclensionFor2,
        string valueDeclensionFor5)
    {
        return
            NumberToText(value, male)
            + " "
            + GetDeclension((int)(value % 10), valueDeclensionFor1, valueDeclensionFor2, valueDeclensionFor5);
    }

    private static string GetDeclension(int val, string one, string two, string five)
    {
        var t = (val % 100 > 20) ? val % 10 : val % 20;

        switch (t)
        {
            case 1:
                return one;
            case 2:
            case 3:
            case 4:
                return two;
            default:
                return five;
        }
    }
}
