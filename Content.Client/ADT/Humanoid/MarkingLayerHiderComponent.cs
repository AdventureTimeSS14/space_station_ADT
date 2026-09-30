using Content.Shared.Humanoid;

namespace Content.Client.ADT.Humanoid;

[RegisterComponent]
public sealed partial class MarkingLayerHiderComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<HumanoidVisualLayers, HashSet<EntityUid>> HiddenBy = new();
}
