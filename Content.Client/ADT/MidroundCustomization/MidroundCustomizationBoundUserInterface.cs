using Content.Client.Humanoid;
using Content.Shared.ADT.MidroundCustomization;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.MidroundCustomization;

public sealed class MidroundCustomizationBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MidroundCustomizationWindow? _window;
    private MarkingsViewModel _markingsModel = new();

    public MidroundCustomizationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _markingsModel = new MarkingsViewModel();
        _window = this.CreateWindow<MidroundCustomizationWindow>();
        _window.MarkingsPicker.SetModel(_markingsModel);
        _markingsModel.MarkingsChanged += (_, _) => SendMarkingSet();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MidroundCustomizationUiState data || _window == null)
        {
            return;
        }

        if (!OrganProfileDataEquals(_markingsModel.OrganProfileData, data.OrganProfileData))
            _markingsModel.OrganProfileData = data.OrganProfileData;

        if (!OrganDataEquals(_markingsModel.OrganData, data.OrganMarkingData))
            _markingsModel.OrganData = data.OrganMarkingData;

        _markingsModel.Markings = data.AppliedMarkings;
    }

    private static bool OrganDataEquals(
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> a,
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var (key, av) in a)
        {
            if (!b.TryGetValue(key, out var bv) || av.Group != bv.Group || !av.Layers.SetEquals(bv.Layers))
                return false;
        }

        return true;
    }

    private static bool OrganProfileDataEquals(
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> a,
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var (key, av) in a)
        {
            if (!b.TryGetValue(key, out var bv) || !av.Equals(bv))
                return false;
        }

        return true;
    }

    private void SendMarkingSet()
    {
        SendMessage(new MidroundCustomizationSelectMessage(_markingsModel.Markings));
    }
}
