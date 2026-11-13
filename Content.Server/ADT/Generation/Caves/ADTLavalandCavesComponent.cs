using System.Threading.Tasks;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Generation.Caves;

[RegisterComponent]
public sealed partial class ADTLavalandCavesComponent : Component
{
    [DataField]
    public float ClosedChance = 45f;

    [DataField]
    public int SmoothingIterations = 50;

    [DataField]
    public int BirthLimit = 4;

    [DataField]
    public int DeathLimit = 3;

    [DataField]
    public int MinNodes = 6;

    [DataField]
    public int MaxNodes = 10;

    [DataField]
    public int NodeMinSize = 75;

    [DataField]
    public int NodeMaxSize = 90;

    [DataField]
    public ADTCaveNoiseSettings HardNoise = new();

    [DataField]
    public ADTCaveNoiseSettings DoubleNoise = new();

    [DataField(required: true)]
    public List<ADTCaveWallType> Walls = new();

    [DataField]
    public Dictionary<string, float> Spread = new();

    [DataField]
    public float ExtraChance;

    [DataField]
    public Dictionary<string, float> ExtraOres = new();

    [ViewVariables]
    public Task<ADTCaveData>? Data;

    [ViewVariables]
    public string[][] Prototypes = Array.Empty<string[]>();
}

[DataDefinition]
public sealed partial class ADTCaveNoiseSettings
{
    [DataField]
    public float Chance = 40f;

    [DataField]
    public int Iterations = 100;

    [DataField]
    public int BirthLimit = 4;

    [DataField]
    public int DeathLimit = 3;
}

[DataDefinition]
public sealed partial class ADTCaveWallType
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public float MineralChance;

    [DataField]
    public Dictionary<string, float> Ores = new();
}
