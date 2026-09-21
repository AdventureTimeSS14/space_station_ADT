using Content.Shared.ADT.Construction;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Medical.AdvancedMedBed;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AdvancedMedBedSystem))]
public sealed partial class AdvancedMedBedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MetabolismMultiplier = 1f;
}