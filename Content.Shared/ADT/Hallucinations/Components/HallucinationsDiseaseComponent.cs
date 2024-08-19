using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Hallucinations;

[RegisterComponent]
public sealed partial class HallucinationsDiseaseComponent : Component
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSecond = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 7f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpawnRate = 15f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinChance = 0.1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxChance = 0.8f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float IncreaseChance = 0.1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxSpawns = 3;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int SpawnedCount = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CurChance = 0.1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? EndTime;

    public List<EntProtoId> Spawns = new();

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/ADT/ling-drugs.ogg")
    {
    };

    [DataField]
    public ushort Layer = 50;

    public HallucinationsPrototype? Proto;

    [ValidatePrototypeId<HallucinationsPrototype>]
    public string? HallucinationsPreset;

    [DataField]
    public bool Epidemic = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SpreadTime = TimeSpan.Zero;
}
