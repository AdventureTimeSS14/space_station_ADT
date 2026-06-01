using Content.Client.Humanoid;
using Content.Shared.ADT.IpcScreen;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.IpcScreen;

public sealed class IpcScreenBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private IpcScreenWindow? _window;
    private MarkingsViewModel _markingsModel = new();

    public IpcScreenBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _markingsModel = new MarkingsViewModel();
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
        SendMessage(new IpcScreenSelectMessage(_markingsModel.Markings));
    }
}
