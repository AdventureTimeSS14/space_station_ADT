using Content.Shared.ADT.Construction;

namespace Content.Shared.ADT.Medical.AdvancedMedBed;

[RegisterComponent]
[Access(typeof(AdvancedMedBedSystem))]
public sealed partial class AdvancedMedBedComponent : Component
{
    [DataField]
    public float MetabolismMultiplier = 1f;
}