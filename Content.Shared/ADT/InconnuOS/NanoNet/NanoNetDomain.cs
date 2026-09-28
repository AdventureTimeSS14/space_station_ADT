namespace Content.Shared.ADT.InconnuOS.NanoNet;

public static class NanoNetDomain
{
    public const string ApexHost = "nanonet.nt";

    public const int MinLabelLength = 3;
    public const int MaxLabelLength = 24;

    public static readonly HashSet<string> ReservedLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "nanonet", "www", "news", "about", "mail", "ftp", "admin", "root", "system", "api", "null", "localhost", "test",
    };

    public static bool TryNormalizeLabel(string input, out string label, out OsValidationError error)
    {
        label = input.Trim().ToLowerInvariant();
        error = OsValidationError.None;

        if (label.Length < MinLabelLength || label.Length > MaxLabelLength)
        {
            error = OsValidationError.DomainInvalid;
            return false;
        }

        if (!char.IsAsciiLetter(label[0]))
        {
            error = OsValidationError.DomainInvalid;
            return false;
        }

        foreach (var c in label)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '-')
            {
                error = OsValidationError.DomainInvalid;
                return false;
            }
        }

        if (ReservedLabels.Contains(label))
        {
            error = OsValidationError.DomainReserved;
            return false;
        }

        return true;
    }

    public static string GetHost(string label)
    {
        return $"{label}.nt";
    }
}
