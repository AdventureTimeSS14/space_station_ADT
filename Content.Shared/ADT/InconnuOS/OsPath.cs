namespace Content.Shared.ADT.InconnuOS;

public static class OsPath
{
    public const char Separator = '/';

    public const StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

    public static readonly char[] InvalidNameChars =
    {
        '\\', '/', ':', '*', '?', '"', '<', '>', '|',
    };

    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (name == "." || name == "..")
            return false;

        foreach (var c in name)
        {
            if (char.IsControl(c))
                return false;

            if (Array.IndexOf(InvalidNameChars, c) >= 0)
                return false;
        }

        return true;
    }

    public static bool IsDriveLetter(char c)
    {
        return c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    public static string GetRoot(char drive)
    {
        return $"{char.ToUpperInvariant(drive)}:{Separator}";
    }

    public static bool TryNormalize(string? path, out string result)
    {
        result = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
            return false;

        var raw = path.Trim().Replace('\\', Separator);

        if (raw.Length < 2 || raw[1] != ':' || !IsDriveLetter(raw[0]))
            return false;

        var drive = char.ToUpperInvariant(raw[0]);
        var rest = raw[2..];

        var segments = new List<string>();

        foreach (var part in rest.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);

                continue;
            }

            if (!IsValidName(part))
                return false;

            segments.Add(part);
        }

        result = GetRoot(drive) + string.Join(Separator, segments);
        return true;
    }

    public static char GetDriveLetter(string path)
    {
        if (path.Length < 1 || !IsDriveLetter(path[0]))
            return '?';

        return char.ToUpperInvariant(path[0]);
    }

    public static bool IsRoot(string path)
    {
        return path.Length == 3 && path[1] == ':' && path[2] == Separator;
    }

    public static string GetName(string path)
    {
        if (IsRoot(path))
            return path;

        var index = path.LastIndexOf(Separator);

        if (index < 0 || index == path.Length - 1)
            return path;

        return path[(index + 1)..];
    }

    public static string GetExtension(string path)
    {
        var name = GetName(path);
        var index = name.LastIndexOf('.');

        if (index <= 0 || index == name.Length - 1)
            return string.Empty;

        return name[index..].ToLowerInvariant();
    }

    public static string GetNameWithoutExtension(string path)
    {
        var name = GetName(path);
        var extension = GetExtension(path);

        if (extension.Length == 0)
            return name;

        return name[..^extension.Length];
    }

    public static string GetParent(string path)
    {
        if (IsRoot(path))
            return path;

        var index = path.LastIndexOf(Separator);

        if (index < 0)
            return path;

        if (index == 2)
            return path[..3];

        return path[..index];
    }

    public static string Combine(string parent, string name)
    {
        if (parent.Length == 0)
            return name;

        if (parent[^1] == Separator)
            return parent + name;

        return parent + Separator + name;
    }

    public static bool IsInside(string path, string directory)
    {
        if (path.Equals(directory, Comparison))
            return false;

        if (IsRoot(directory))
            return GetDriveLetter(path) == GetDriveLetter(directory);

        if (!path.StartsWith(directory, Comparison))
            return false;

        return path.Length > directory.Length && path[directory.Length] == Separator;
    }

    public static bool IsDirectlyInside(string path, string directory)
    {
        return GetParent(path).Equals(directory, Comparison);
    }

    public static OsFileKind KindFromExtension(string path)
    {
        return GetExtension(path) switch
        {
            ".lgc" => OsFileKind.Circuit,
            ".log" => OsFileKind.Log,
            ".lnk" => OsFileKind.Shortcut,
            _ => OsFileKind.Text,
        };
    }

    public static string ExtensionForKind(OsFileKind kind)
    {
        return kind switch
        {
            OsFileKind.Circuit => ".lgc",
            OsFileKind.Log => ".log",
            OsFileKind.Shortcut => ".lnk",
            OsFileKind.Directory => string.Empty,
            _ => ".txt",
        };
    }
}
