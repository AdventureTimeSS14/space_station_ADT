using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Medical.AdvancedMedBed;

[RegisterComponent, NetworkedComponent]
[Access(typeof(AdvancedMedBedSystem))]
public sealed partial class AdvancedMedBedBuckledComponent : Component;