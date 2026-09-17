using Content.Shared.ADT.InconnuOS.Components;
using Content.Shared.ADT.LogicCircuit;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.InconnuOS;

public abstract class SharedADTOsSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Prototypes = default!;

    public OsLimits GetLimits(ADTOperatingSystemComponent component)
    {
        return new OsLimits
        {
            MaxFiles = component.MaxFiles,
            MaxFileLength = component.MaxFileLength,
            MaxPathLength = component.MaxPathLength,
            MaxTotalLength = component.MaxTotalLength,
        };
    }

    public bool TryNormalizePath(
        string? path,
        OsLimits limits,
        out string normalized,
        out OsValidationError error,
        out string detail)
    {
        error = OsValidationError.None;
        detail = string.Empty;

        if (!OsPath.TryNormalize(path, out normalized))
        {
            error = OsValidationError.InvalidPath;
            detail = path ?? string.Empty;
            return false;
        }

        if (normalized.Length > limits.MaxPathLength)
        {
            error = OsValidationError.PathTooLong;
            detail = limits.MaxPathLength.ToString();
            return false;
        }

        return true;
    }

    private static bool TryCheckParents(
        OsDisk disk,
        string path,
        out OsValidationError error,
        out string detail)
    {
        error = OsValidationError.None;
        detail = string.Empty;

        foreach (var file in disk.Files)
        {
            if (file.IsDirectory)
                continue;

            if (!OsPath.IsInside(path, file.Path))
                continue;

            error = OsValidationError.NotADirectory;
            detail = file.Path;
            return false;
        }

        return true;
    }

    public bool TryWrite(
        OsDisk disk,
        OsLimits limits,
        string path,
        OsFileKind kind,
        string text,
        LogicCircuitLayout? circuit,
        TimeSpan now,
        out OsValidationError error,
        out string detail)
    {
        if (!TryNormalizePath(path, limits, out var full, out error, out detail))
            return false;

        if (OsPath.IsRoot(full))
        {
            error = OsValidationError.InvalidPath;
            detail = full;
            return false;
        }

        if (text.Length > limits.MaxFileLength)
        {
            error = OsValidationError.FileTooLong;
            detail = limits.MaxFileLength.ToString();
            return false;
        }

        if (kind == OsFileKind.Circuit && circuit == null)
        {
            error = OsValidationError.BadCircuit;
            detail = OsPath.GetName(full);
            return false;
        }

        if (!TryCheckParents(disk, full, out error, out detail))
            return false;

        var index = disk.IndexOf(full);
        var existing = index < 0 ? null : disk.Files[index];

        if (existing != null && existing.ReadOnly)
        {
            error = OsValidationError.FileReadOnly;
            detail = existing.Name;
            return false;
        }

        if (existing != null && existing.IsDirectory)
        {
            error = OsValidationError.FileExists;
            detail = existing.Name;
            return false;
        }

        if (existing == null && disk.Count >= limits.MaxFiles)
        {
            error = OsValidationError.TooManyFiles;
            detail = limits.MaxFiles.ToString();
            return false;
        }

        var file = new OsFile
        {
            Path = full,
            Kind = kind,
            Text = text,
            Circuit = circuit?.Clone(),
            Modified = now,
        };

        var total = disk.TotalSize - (existing?.Size ?? 0) + file.Size;

        if (total > limits.MaxTotalLength)
        {
            error = OsValidationError.DiskFull;
            detail = limits.MaxTotalLength.ToString();
            return false;
        }

        if (index < 0)
        {
            disk.Files.Add(file);
        }
        else
        {
            disk.Files[index] = file;
        }

        error = OsValidationError.None;
        detail = string.Empty;
        return true;
    }

    public bool TryCreateDirectory(
        OsDisk disk,
        OsLimits limits,
        string path,
        TimeSpan now,
        out OsValidationError error,
        out string detail)
    {
        if (!TryNormalizePath(path, limits, out var full, out error, out detail))
            return false;

        if (OsPath.IsRoot(full))
        {
            error = OsValidationError.InvalidPath;
            detail = full;
            return false;
        }

        if (!TryCheckParents(disk, full, out error, out detail))
            return false;

        if (disk.Contains(full))
        {
            error = OsValidationError.FileExists;
            detail = OsPath.GetName(full);
            return false;
        }

        if (disk.Count >= limits.MaxFiles)
        {
            error = OsValidationError.TooManyFiles;
            detail = limits.MaxFiles.ToString();
            return false;
        }

        disk.Files.Add(new OsFile
        {
            Path = full,
            Kind = OsFileKind.Directory,
            Modified = now,
        });

        return true;
    }

    public bool TryDelete(
        OsDisk disk,
        OsLimits limits,
        string path,
        bool recursive,
        out OsValidationError error,
        out string detail)
    {
        if (!TryNormalizePath(path, limits, out var full, out error, out detail))
            return false;

        var index = disk.IndexOf(full);
        var children = CollectChildren(disk, full);

        if (index < 0 && children.Count == 0)
        {
            error = OsValidationError.FileNotFound;
            detail = OsPath.GetName(full);
            return false;
        }

        if (index >= 0 && disk.Files[index].ReadOnly)
        {
            error = OsValidationError.FileReadOnly;
            detail = disk.Files[index].Name;
            return false;
        }

        if (children.Count > 0 && !recursive)
        {
            error = OsValidationError.DirectoryNotEmpty;
            detail = OsPath.GetName(full);
            return false;
        }

        foreach (var child in children)
        {
            if (!child.ReadOnly)
                continue;

            error = OsValidationError.FileReadOnly;
            detail = child.Name;
            return false;
        }

        foreach (var child in children)
        {
            disk.Files.Remove(child);
        }

        if (index >= 0)
            disk.Files.RemoveAt(disk.IndexOf(full));

        error = OsValidationError.None;
        detail = string.Empty;
        return true;
    }

    public bool TryMove(
        OsDisk sourceDisk,
        OsDisk targetDisk,
        OsLimits limits,
        string source,
        string target,
        bool copy,
        TimeSpan now,
        out OsValidationError error,
        out string detail)
    {
        if (!TryNormalizePath(source, limits, out var full, out error, out detail))
            return false;

        if (!TryNormalizePath(target, limits, out var dest, out error, out detail))
            return false;

        if (full.Equals(dest, OsPath.Comparison))
        {
            error = OsValidationError.FileExists;
            detail = OsPath.GetName(dest);
            return false;
        }

        if (OsPath.IsInside(dest, full))
        {
            error = OsValidationError.RecursiveMove;
            detail = OsPath.GetName(full);
            return false;
        }

        var index = sourceDisk.IndexOf(full);
        var children = CollectChildren(sourceDisk, full);

        if (index < 0 && children.Count == 0)
        {
            error = OsValidationError.FileNotFound;
            detail = OsPath.GetName(full);
            return false;
        }

        if (!copy && index >= 0 && sourceDisk.Files[index].ReadOnly)
        {
            error = OsValidationError.FileReadOnly;
            detail = sourceDisk.Files[index].Name;
            return false;
        }

        if (!TryCheckParents(targetDisk, dest, out error, out detail))
            return false;

        if (targetDisk.Contains(dest))
        {
            error = OsValidationError.FileExists;
            detail = OsPath.GetName(dest);
            return false;
        }

        var moved = new List<OsFile>();

        if (index >= 0)
            moved.Add(Retarget(sourceDisk.Files[index], full, dest, now));

        foreach (var child in children)
        {
            moved.Add(Retarget(child, full, dest, now));
        }

        if (targetDisk.Count + moved.Count > limits.MaxFiles)
        {
            error = OsValidationError.TooManyFiles;
            detail = limits.MaxFiles.ToString();
            return false;
        }

        var added = 0;

        foreach (var file in moved)
        {
            added += file.Size;
        }

        var removed = 0;

        if (!copy && ReferenceEquals(sourceDisk, targetDisk))
        {
            foreach (var file in moved)
            {
                removed += file.Size;
            }
        }

        if (targetDisk.TotalSize - removed + added > limits.MaxTotalLength)
        {
            error = OsValidationError.DiskFull;
            detail = limits.MaxTotalLength.ToString();
            return false;
        }

        foreach (var file in moved)
        {
            if (file.Path.Length <= limits.MaxPathLength)
                continue;

            error = OsValidationError.PathTooLong;
            detail = limits.MaxPathLength.ToString();
            return false;
        }

        if (!copy)
        {
            foreach (var child in children)
            {
                sourceDisk.Files.Remove(child);
            }

            if (index >= 0)
                sourceDisk.Files.RemoveAt(sourceDisk.IndexOf(full));
        }

        targetDisk.Files.AddRange(moved);

        error = OsValidationError.None;
        detail = string.Empty;
        return true;
    }

    private static OsFile Retarget(OsFile file, string sourceRoot, string targetRoot, TimeSpan now)
    {
        var clone = file.Clone();

        clone.Path = file.Path.Equals(sourceRoot, OsPath.Comparison)
            ? targetRoot
            : targetRoot + file.Path[sourceRoot.Length..];

        clone.ReadOnly = false;
        clone.Critical = false;
        clone.Modified = now;

        return clone;
    }

    public static bool ContainsCritical(OsDisk disk, string path)
    {
        if (!OsPath.TryNormalize(path, out var full))
            return false;

        foreach (var file in disk.Files)
        {
            if (!file.Critical)
                continue;

            if (file.Path.Equals(full, OsPath.Comparison) || OsPath.IsInside(file.Path, full))
                return true;
        }

        return false;
    }

    private static List<OsFile> CollectChildren(OsDisk disk, string directory)
    {
        var result = new List<OsFile>();

        foreach (var file in disk.Files)
        {
            if (OsPath.IsInside(file.Path, directory))
                result.Add(file);
        }

        return result;
    }
}
