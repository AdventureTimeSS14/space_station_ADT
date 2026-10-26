using Robust.Shared.GameStates;
using Robust.Shared.Localization;

namespace Content.Shared.ADT.AshWalker.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTPointOfInterestComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId Title = "adt-point-of-interest-generic";
}
