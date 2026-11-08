using Content.Shared.ADT.InconnuOS;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.InconnuOS.UI;

public abstract class OsAppControl : Control
{
    public OsContext Context = default!;
    public ADTOsAppPrototype Proto = default!;

    public event Action<string>? TitleChanged;

    protected void SetTitle(string title)
    {
        TitleChanged?.Invoke(title);
    }

    public virtual void OnOpen(string? argument)
    {
    }

    public virtual void OnStateChanged()
    {
    }

    public virtual object? SaveState()
    {
        return null;
    }

    public virtual void LoadState(object state)
    {
    }

    public virtual bool OnClosing()
    {
        return true;
    }
}
