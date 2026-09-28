using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Damage.Components;

[RegisterComponent]
public sealed partial class ChangeDamageContainerComponent : Component
{
    public const string BiologicalMetaphysicalContainer = "BiologicalMetaphysical";

    [DataField("containerId")]
    public string ContainerId = BiologicalMetaphysicalContainer;

    public string? OriginalContainerId;
}