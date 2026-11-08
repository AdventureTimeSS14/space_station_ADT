using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.Components;
using Content.Shared.ADT.LogicCircuit;

namespace Content.Server.ADT.InconnuOS;

public sealed partial class ADTOsSystem
{
    private bool TryResolveDrive(
        Entity<ADTOperatingSystemComponent> ent,
        string path,
        out OsDisk disk,
        out OsLimits limits,
        out char letter,
        out OsValidationError error,
        out string detail)
    {
        disk = default!;
        limits = default;
        letter = '?';
        detail = string.Empty;

        if (!OsPath.TryNormalize(path, out var full))
        {
            error = OsValidationError.InvalidPath;
            detail = path;
            return false;
        }

        letter = OsPath.GetDriveLetter(full);

        if (!TryGetDrive(ent, letter, out disk, out limits, out var readOnly, out error))
        {
            detail = letter.ToString();
            return false;
        }

        if (!readOnly)
            return true;

        error = OsValidationError.DriveReadOnly;
        detail = letter.ToString();
        return false;
    }

    public bool TryWriteFile(
        Entity<ADTOperatingSystemComponent> ent,
        string path,
        OsFileKind kind,
        string text,
        LogicCircuitLayout? circuit,
        out OsValidationError error,
        out string detail)
    {
        if (!TryResolveDrive(ent, path, out var disk, out var limits, out var letter, out error, out detail))
            return false;

        if (!TryWrite(disk, limits, path, kind, text, circuit, _timing.CurTime, out error, out detail))
            return false;

        AfterDriveChanged(ent, letter);
        return true;
    }

    public bool TryMakeDirectory(
        Entity<ADTOperatingSystemComponent> ent,
        string path,
        out OsValidationError error,
        out string detail)
    {
        if (!TryResolveDrive(ent, path, out var disk, out var limits, out _, out error, out detail))
            return false;

        return TryCreateDirectory(disk, limits, path, _timing.CurTime, out error, out detail);
    }

    public bool TryDeleteFile(
        Entity<ADTOperatingSystemComponent> ent,
        string path,
        bool recursive,
        out OsValidationError error,
        out string detail)
    {
        if (!TryResolveDrive(ent, path, out var disk, out var limits, out var letter, out error, out detail))
            return false;

        var critical = ContainsCritical(disk, path);

        if (!TryDelete(disk, limits, path, recursive, out error, out detail))
            return false;

        AfterDriveChanged(ent, letter);

        if (critical)
            Crash(ent);

        return true;
    }

    public bool TryMoveFile(
        Entity<ADTOperatingSystemComponent> ent,
        string source,
        string target,
        bool copy,
        out OsValidationError error,
        out string detail)
    {
        if (!TryResolveDrive(ent, source, out var sourceDisk, out _, out var sourceLetter, out error, out detail))
            return false;

        if (!TryResolveDrive(ent, target, out var targetDisk, out var limits, out var targetLetter, out error, out detail))
            return false;

        if (!TryMove(sourceDisk, targetDisk, limits, source, target, copy, _timing.CurTime, out error, out detail))
            return false;

        AfterDriveChanged(ent, sourceLetter);

        if (targetLetter != sourceLetter)
            AfterDriveChanged(ent, targetLetter);

        return true;
    }

    private void InstallDefaultFiles(Entity<ADTOperatingSystemComponent> ent)
    {
        var comp = ent.Comp;

        if (!comp.InstallDefaultFiles || comp.DefaultFilesInstalled)
            return;

        comp.DefaultFilesInstalled = true;

        var root = OsPath.GetRoot(comp.Drive);

        AddDefault(comp.Disk, root + "LICENSE.txt", OsFileKind.Text, Loc.GetString("os-file-license"), true);
        AddDefault(comp.Disk, root + "system.log", OsFileKind.Log, Loc.GetString("os-file-syslog",
            ("machine", comp.MachineName)), true);

        AddDefault(comp.Disk, root + Loc.GetString("os-folder-circuits"), OsFileKind.Directory, string.Empty, false);

        AddDefault(comp.Disk, root + "InconnuOS", OsFileKind.Directory, string.Empty, true);

        RestoreKernel(ent);
    }

    private const long KernelDisplaySize = 20L * 1024 * 1024 * 1024 * 1024 * 1024;

    private static string KernelPath(ADTOperatingSystemComponent comp)
    {
        return OsPath.GetRoot(comp.Drive) + "InconnuOS/kernel";
    }

    private void RestoreKernel(Entity<ADTOperatingSystemComponent> ent)
    {
        var comp = ent.Comp;

        if (!comp.InstallDefaultFiles)
            return;

        var path = KernelPath(comp);

        if (comp.Disk.Contains(path))
            return;

        comp.Disk.Files.Add(new OsFile
        {
            Path = path,
            Kind = OsFileKind.Text,
            Text = Loc.GetString("os-file-kernel"),
            DisplaySize = KernelDisplaySize,
            Critical = true,
            Modified = _timing.CurTime,
        });
    }

    private void AddDefault(OsDisk disk, string path, OsFileKind kind, string text, bool readOnly)
    {
        if (disk.Contains(path))
            return;

        disk.Files.Add(new OsFile
        {
            Path = path,
            Kind = kind,
            Text = text,
            ReadOnly = readOnly,
            Modified = _timing.CurTime,
        });
    }
}
