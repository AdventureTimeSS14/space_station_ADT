using System.Text;

namespace Content.Shared.ADT.Chat;

public static class SharedADTDoEmote
{
    public const string ColorHex = "#D1A928";
    private const string Terminators = ".!?…";
    private static readonly char[] TrailingJunk = { ',', ';', ':' };

    public static string Format(string message)
    {
        message = message.Trim();

        if (string.IsNullOrEmpty(message))
            return string.Empty;

        message = message.TrimEnd(TrailingJunk).TrimEnd();

        if (string.IsNullOrEmpty(message))
            return string.Empty;

        var builder = new StringBuilder(message.Length + 1);

        builder.Append(char.ToUpperInvariant(message[0]));
        builder.Append(message, 1, message.Length - 1);

        if (Terminators.IndexOf(message[message.Length - 1]) < 0)
            builder.Append('.');

        return builder.ToString();
    }
}
