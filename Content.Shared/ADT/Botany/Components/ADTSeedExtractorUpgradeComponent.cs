using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Botany.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTSeedExtractorUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SeedMultiplier = 1f;
}
