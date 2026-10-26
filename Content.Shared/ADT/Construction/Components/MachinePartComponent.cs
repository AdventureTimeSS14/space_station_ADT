using Content.Shared.ADT.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction.Components;

/// <summary>
/// Marks an entity as a machine part (capacitor, servo, matter bin, etc).
/// The part type and tier are used by machines to apply upgrades.
/// </summary>
[RegisterComponent]
public sealed partial class MachinePartComponent : Component
{
    [DataField(required: true)]
    public ProtoId<MachinePartPrototype> Part;

    [DataField]
    public int Tier = 1;
}
