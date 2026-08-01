using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Clothing;

/// <summary>
///     Added to clothing that has been coated with something
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CoatedClothingComponent : Component
{
    [DataField]
    public List<string> CoatingNames = new();
}