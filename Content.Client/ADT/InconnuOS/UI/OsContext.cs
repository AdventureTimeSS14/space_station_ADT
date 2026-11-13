using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsContext
{
    public readonly IPrototypeManager Prototypes;
    public readonly IResourceCache Cache;
    public readonly IGameTiming Timing;
    public readonly IEntityManager Entities;

    public ADTOsBuiState State { get; private set; }

    public Action<BoundUserInterfaceMessage> Send = _ => { };
    public Action<string, string?> OpenApp = (_, _) => { };
    public Action<string> OpenFile = _ => { };
    public Action<string> Toast = _ => { };

    public Action<Vector2, List<OsMenuEntry>> ShowMenu = (_, _) => { };

    public Func<IReadOnlyList<OsWindow>> Windows = () => Array.Empty<OsWindow>();

    public Action<OsWindow> CloseWindow = _ => { };
    public Action<OsWindow> FocusWindow = _ => { };

    public event Action? WindowsChanged;

    public void RaiseWindowsChanged()
    {
        WindowsChanged?.Invoke();
    }

    public event Action<BoundUserInterfaceMessage>? MessageReceived;

    public Color Accent => State.Settings.Accent;

    public OsContext(
        IPrototypeManager prototypes,
        IResourceCache cache,
        IGameTiming timing,
        IEntityManager entities,
        ADTOsBuiState state)
    {
        Prototypes = prototypes;
        Cache = cache;
        Timing = timing;
        Entities = entities;
        State = state;
    }

    public TimeSpan Uptime
    {
        get
        {
            if (State.BootedAt == TimeSpan.Zero)
                return TimeSpan.Zero;

            var uptime = Timing.CurTime - State.BootedAt;

            return uptime < TimeSpan.Zero ? TimeSpan.Zero : uptime;
        }
    }

    public void SetState(ADTOsBuiState state)
    {
        State = state;
    }

    public void Receive(BoundUserInterfaceMessage message)
    {
        MessageReceived?.Invoke(message);
    }

    public OsDriveState? GetDrive(char letter)
    {
        var upper = char.ToUpperInvariant(letter);

        foreach (var drive in State.Drives)
        {
            if (char.ToUpperInvariant(drive.Letter) == upper)
                return drive;
        }

        return null;
    }

    public OsDriveState? GetDriveOf(string path)
    {
        return GetDrive(OsPath.GetDriveLetter(path));
    }

    public OsFile? FindFile(string path)
    {
        return GetDriveOf(path)?.Disk.Find(path);
    }

    public List<OsFile> List(string directory)
    {
        if (GetDriveOf(directory) is not { } drive)
            return new List<OsFile>();

        return drive.Disk.List(directory);
    }

    public bool IsWritable(string path)
    {
        if (GetDriveOf(path) is not { } drive)
            return false;

        return !drive.ReadOnly;
    }

    public ADTOsAppPrototype? FindHandler(string path)
    {
        var extension = OsPath.GetExtension(path);

        foreach (var id in State.Apps)
        {
            if (!Prototypes.TryIndex(id, out var app))
                continue;

            foreach (var handled in app.Handles)
            {
                if (handled.Equals(extension, StringComparison.OrdinalIgnoreCase))
                    return app;
            }
        }

        return null;
    }
}
