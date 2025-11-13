using Content.Shared.ADT.Pinpointer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.ADT.Pinpointer.UI;

[UsedImplicitly]
public sealed class ObjectivePinpointerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ObjectivePinpointerWindow? _window;

    public ObjectivePinpointerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new ObjectivePinpointerWindow();
        _window.OnTargetSelected += OnTargetSelected;
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ObjectivePinpointerBoundUserInterfaceState cast)
            return;

        _window?.UpdateTargets(cast.Targets);
    }

    private void OnTargetSelected(NetEntity target)
    {
        SendMessage(new ObjectivePinpointerSelectMessage(target));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Close();
        }
    }
}