using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

/// <summary>
/// Marker component for items that were created as part of a heretic flesh mimic clone.
/// Items with this component are deleted instead of dropped when the clone dies,
/// preventing item duplication.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HereticCloneItemComponent : Component
{
}
