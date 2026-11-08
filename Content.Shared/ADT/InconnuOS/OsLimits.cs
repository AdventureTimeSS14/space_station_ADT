using System.Text.RegularExpressions;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.InconnuOS;

[Serializable, NetSerializable]
public struct OsLimits
{
    public int MaxFiles;
    public int MaxFileLength;
    public int MaxPathLength;
    public int MaxTotalLength;
}

[Serializable, NetSerializable]
public enum OsValidationError : byte
{
    None = 0,
    InvalidPath,
    PathTooLong,
    UnknownDrive,
    DriveReadOnly,
    FileNotFound,
    FileExists,
    FileReadOnly,
    NotADirectory,
    DirectoryNotEmpty,
    TooManyFiles,
    FileTooLong,
    DiskFull,
    BadCircuit,
    RecursiveMove,
}

public static class OsErrors
{
    public static string GetMessage(OsValidationError error, string detail)
    {
        var name = Enum.GetName(error) ?? "unknown";
        var key = Regex.Replace(name, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();

        return Loc.GetString($"os-error-{key}", ("detail", detail));
    }
}
