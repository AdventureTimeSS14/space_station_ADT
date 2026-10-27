using Content.Shared.ADT.Surgery.Events;
using JetBrains.Annotations;

namespace Content.Client.ADT.Surgery.UI;

[UsedImplicitly]
public sealed class SurgeryBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SurgeryWindow? _window;

    public SurgeryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case SurgeryChooseGraphState graphState:
                EnsureWindow().Populate(graphState);
                _window!.OpenCentered();
                break;

            case SurgeryChooseEdgeState edgeState:
                EnsureWindow().Populate(edgeState);
                _window!.OpenCentered();
                break;
        }
    }

    private SurgeryWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = new SurgeryWindow();
        _window.OnClose += Close;
        _window.OnGraphSelected += OnGraphSelected;
        _window.OnEdgeSelected += OnEdgeSelected;
        return _window;
    }

    private void OnGraphSelected(string graphId)
    {
        SendMessage(new SurgerySelectGraphMessage(graphId));
        _window?.Close();
    }

    private void OnEdgeSelected(string edgeId)
    {
        SendMessage(new SurgerySelectEdgeMessage(edgeId));
        _window?.Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
        {
            _window.OnClose -= Close;
            _window.OnGraphSelected -= OnGraphSelected;
            _window.OnEdgeSelected -= OnEdgeSelected;
            _window.Dispose();
        }
    }
}
