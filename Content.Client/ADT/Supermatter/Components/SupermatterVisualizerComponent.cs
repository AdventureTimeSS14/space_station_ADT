using Content.Client.ADT.Supermatter.Systems;
using Content.Shared.ADT.Supermatter.Components;

namespace Content.Client.ADT.Supermatter.Components;

[RegisterComponent]
[Access(typeof(SupermatterVisualizerSystem))]
public sealed partial class SupermatterVisualsComponent : Component
{
    [DataField("crystal", required: true)]
    public Dictionary<SupermatterCrystalState, PrototypeLayerData> CrystalVisuals = default!;
}
