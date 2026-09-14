using System.Text.RegularExpressions;

namespace Content.Shared.ADT.TenCodes;

public static class ADTTenCodes
{
    public const string MarkupTag = "tencode";
    public const string Prefix = "10-";

    public static readonly Regex CodeRegex = new(@"\b10-\d{1,3}\b", RegexOptions.Compiled);
}
