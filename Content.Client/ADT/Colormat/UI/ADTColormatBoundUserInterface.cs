using Content.Shared.ADT.Colormat;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Colormat.UI;

[UsedImplicitly]
public sealed class ADTColormatBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ADTColormatWindow? _window;

    public ADTColormatBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new ADTColormatWindow();
        _window.OnEject += () => SendMessage(new ADTColormatEjectMessage());
        _window.OnSaveColor += color => SendMessage(new ADTColormatSetColorMessage(color));
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ADTColormatUiState uiState)
            return;

        EntityUid? item = uiState.Item is { } net ? EntMan.GetEntity(net) : null;
        _window?.Update(item);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window == null)
            return;

        _window.OnClose -= Close;
        _window.Dispose();
    }
}
