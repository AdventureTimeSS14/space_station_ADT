using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared.ADT.Components.PickupHumans;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TakenHumansComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid Carrier;

    [ViewVariables]
    public BodyType OriginalBodyType = BodyType.KinematicController;

    [ViewVariables]
    public DoAfterId? EscapeDoAfter;
}
