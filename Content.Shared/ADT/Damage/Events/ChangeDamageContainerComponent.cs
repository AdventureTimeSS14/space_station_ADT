using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Damage.Components;

/// <summary>
/// Changes the damage container of an entity to a specified one.
/// </summary>
[RegisterComponent]
public sealed partial class ChangeDamageContainerComponent : Component
{
    /// <summary>
    /// The damage container ID to change to.
    /// </summary>
    [DataField("containerId", required: true)]
    public string ContainerId = "BiologicalMetaphysical";

    /// <summary>
    /// The original damage container ID before the change.
    /// </summary>
    public string? OriginalContainerId;
}