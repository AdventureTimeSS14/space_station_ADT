using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Puzzle;

[RegisterComponent]
public sealed partial class ADTSlidingPuzzleComponent : Component
{
    [ViewVariables]
    public List<EntityUid> Elements = new();

    [ViewVariables, DataField]
    public int EmptyTileId = 0;

    [ViewVariables]
    public bool Finished = false;

    [DataField]
    public string ElementProtoPrefix = "ADTPuzzlePillar";

    [DataField]
    public EntProtoId? RewardProto;

    [DataField]
    public string PieceProtoPrefix = "ADTPuzzlePieceFloor";

    /// <summary>
    /// Шанс заспавнить мегафауну в дополнение к награде, если её нет на карте.
    /// </summary>
    [DataField]
    public float MegafaunaChance = 0.25f;

    [DataField]
    public EntProtoId? MegafaunaProto;

    [DataField]
    public float RecoilRadius = 7f;

    [DataField]
    public float RecoilStrength = 2f;

    [DataField]
    public SoundSpecifier? SolveSound;

    [ViewVariables]
    public EntityUid? Prisoner;

    [DataField]
    public EntProtoId? ReturnCubeProto;

    [ViewVariables]
    public EntityUid? GeneratedGrid;
}

[RegisterComponent]
public sealed partial class ADTSlidingPuzzleElementComponent : Component
{
    [ViewVariables]
    public int Id = 0;

    [ViewVariables]
    public EntityUid Source;
}

[RegisterComponent]
public sealed partial class ADTPrisonCubeComponent : Component
{
    [DataField]
    public EntProtoId PuzzleProto = "ADTPrisonCubePuzzle";

    [DataField]
    public SoundSpecifier ActivateSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}