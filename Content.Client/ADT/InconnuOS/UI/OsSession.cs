using System.Numerics;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsSession
{
    public TimeSpan BootedAt;

    public readonly List<OsWindowSave> Windows = new();
}

public sealed class OsWindowSave
{
    public string AppId = string.Empty;
    public Vector2 Position;
    public Vector2 Size;
    public bool Minimized;
    public bool Maximized;
    public object? State;
}
