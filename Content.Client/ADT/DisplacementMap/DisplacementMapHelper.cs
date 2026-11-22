namespace Content.Client.ADT.DisplacementMap;

public static class DisplacementMapHelper
{
    public static bool IsDisplacementKey(string key)
    {
        return key.EndsWith("-displacement", StringComparison.Ordinal);
    }
}