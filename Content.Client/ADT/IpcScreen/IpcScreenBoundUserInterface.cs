using Content.Client.Humanoid;
using Content.Shared.ADT.IpcScreen;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.IpcScreen;

public sealed class IpcScreenBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private IpcScreenWindow? _window;
    private readonly MarkingsViewModel _markingsModel = new();

    public IpcScreenBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<IpcScreenWindow>();
        _window.MarkingsPicker.SetModel(_markingsModel);
        _markingsModel.MarkingsChanged += (_, _) => SendMarkingSet();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not IpcScreenUiState data || _window == null)
        {
            return;
        }

        _markingsModel.OrganData = data.OrganMarkingData;
        _markingsModel.OrganProfileData = data.OrganProfileData;
        _markingsModel.Markings = data.AppliedMarkings;
    }

    private void SendMarkingSet()
    {
        SendMessage(new IpcScreenSelectMessage(_markingsModel.Markings));
    }
}
