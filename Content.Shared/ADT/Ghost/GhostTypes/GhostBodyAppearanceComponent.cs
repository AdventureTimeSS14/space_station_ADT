using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Ghost.GhostTypes;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GhostBodyAppearanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<HumanoidVisualLayers, PrototypeLayerData> Layers = new();

    [DataField, AutoNetworkedField]
    public Dictionary<HumanoidVisualLayers, List<Marking>> Markings = new();

    [DataField, AutoNetworkedField]
    public Sex Sex = Sex.Unsexed;

    [DataField, AutoNetworkedField]
    public Dictionary<string, DisplacementData> Displacements = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, DisplacementData> FemaleDisplacements = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, DisplacementData> MaleDisplacements = new();
}
