using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Utility;

public static class MarkupHelpers
{
    public static bool TryGetLong(MarkupParameter parameter, [NotNullWhen(true)] out long? value)
    {
        if (parameter.TryGetLong(out value))
            return true;

        if (parameter.TryGetString(out var stringValue) && long.TryParse(stringValue, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }
}
