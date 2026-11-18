using Content.Client.UserInterface.Fragments;
using Content.Shared.ADT.Mech;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Mech.UI;

public sealed partial class MechGunUi : UIFragment
{
    private MechGunUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        if (fragmentOwner == null)
            return;

        var entManager = IoCManager.Resolve<IEntityManager>();
        var owner = entManager.GetNetEntity(fragmentOwner.Value);

        _fragment = new MechGunUiFragment
        {
            FragmentOwner = fragmentOwner,
        };

        _fragment.ReloadAction += () => userInterface.SendPredictedMessage(new MechGunReloadMessage(owner));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
    }
}
