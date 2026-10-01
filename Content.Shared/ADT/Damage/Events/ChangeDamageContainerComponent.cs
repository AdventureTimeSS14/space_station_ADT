using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Damage.Components;

[RegisterComponent]
public sealed partial class ChangeDamageContainerComponent : Component
{
    [DataField("containerId")]
    public string ContainerId = "BiologicalMetaphysical";

    public string? OriginalContainerId;
}