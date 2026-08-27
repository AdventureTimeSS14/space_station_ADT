using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Construction.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArtifactAnalyzerUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float PointMultiplier = 1f;
}
