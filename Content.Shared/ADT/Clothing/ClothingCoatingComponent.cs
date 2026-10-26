using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Clothing;

/// <summary>
///     Add this to an item that can be used to coat clothing with something
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingCoatingComponent : Component
{
    [DataField(required: true)]
    public string CoatingName = string.Empty;

    [DataField(required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();
}