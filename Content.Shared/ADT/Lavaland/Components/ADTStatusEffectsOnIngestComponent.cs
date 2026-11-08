using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTStatusEffectsOnIngestComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, TimeSpan> Effects = new();
}
