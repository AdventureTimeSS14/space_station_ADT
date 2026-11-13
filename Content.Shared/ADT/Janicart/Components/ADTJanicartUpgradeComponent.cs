using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Janicart.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedADTJanicartSystem))]
public sealed partial class ADTJanicartUpgradeComponent : Component
{
    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    [DataField]
    public int MaximumOfType = 1;

    [DataField]
    public LocId ExamineText = string.Empty;
}