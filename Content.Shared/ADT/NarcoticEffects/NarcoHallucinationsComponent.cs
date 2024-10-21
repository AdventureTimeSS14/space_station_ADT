using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Content.Shared.Eye;

namespace Content.Shared.NarcoticEffects.Components;

[RegisterComponent]
public sealed partial class NarcoHallucinationsComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSecond = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 7f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpawnRate = 15f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Chance = 0.5f;
    public List<EntProtoId> Spawns = new List<EntProtoId> { "ADTHallucinationMobEcho", "ADTHallucinationMobDistorted" };

}
