using Content.Client.Humanoid;
using Content.Shared.ADT.SlimeHair;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.SlimeHair;

public sealed class SlimeHairBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SlimeHairWindow? _window;
    private readonly MarkingsViewModel _markingsModel = new();

    public SlimeHairBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SlimeHairWindow>();
        _window.MarkingsPicker.SetModel(_markingsModel);
        _markingsModel.MarkingsChanged += (_, _) => SendMarkingSet();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SlimeHairUiState data || _window == null)
        {
            return;
        }

        _markingsModel.OrganData = data.OrganMarkingData;
        _markingsModel.OrganProfileData = data.OrganProfileData;
        _markingsModel.Markings = data.AppliedMarkings;
    }

    private void SendMarkingSet()
    {
        SendMessage(new SlimeHairSelectMessage(_markingsModel.Markings));
    }
}
