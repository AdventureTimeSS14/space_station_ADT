using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland.Helios;

[Serializable, NetSerializable]
public enum ADTHeliosPuzzleVisuals : byte
{
    Lit,
}

[Serializable, NetSerializable]
public enum ADTHeliosPuzzleLayers : byte
{
    Base,
}

[RegisterComponent]
public sealed partial class ADTLightsOutPuzzleComponent : Component
{
    [DataField]
    public int Width = 4;

    [DataField]
    public int Height = 4;

    [DataField]
    public EntProtoId PanelProto = "ADTHeliosLightsPanel";

    [DataField]
    public int Scramble = 7;

    [DataField]
    public ProtoId<SourcePortPrototype>? SolvedPort = "ADTPuzzleSolved";

    [DataField]
    public LocId SolvedMessage = "helios-puzzle-lights-solved";

    [DataField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

    [DataField]
    public SoundSpecifier? SolveSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [ViewVariables]
    public List<EntityUid> Panels = new();

    [ViewVariables]
    public List<bool> Lit = new();

    [ViewVariables]
    public List<int> ScramblePresses = new();

    [ViewVariables]
    public bool Finished;
}

[RegisterComponent]
public sealed partial class ADTLightsOutPanelComponent : Component
{
    [ViewVariables]
    public EntityUid Source;

    [ViewVariables]
    public int Index;
}

[RegisterComponent]
public sealed partial class ADTConduitPuzzleComponent : Component
{
    [DataField]
    public int Width = 5;

    [DataField]
    public int Height = 4;

    [DataField]
    public int EntryRow = 0;

    [DataField]
    public int ExitRow = 3;

    [DataField]
    public EntProtoId StraightProto = "ADTHeliosConduitStraight";

    [DataField]
    public EntProtoId BendProto = "ADTHeliosConduitBend";

    [DataField]
    public EntProtoId TeeProto = "ADTHeliosConduitTee";

    [DataField]
    public ProtoId<SourcePortPrototype>? SolvedPort = "ADTPuzzleSolved";

    [DataField]
    public LocId SolvedMessage = "helios-puzzle-conduit-solved";

    [DataField]
    public SoundSpecifier? RotateSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public SoundSpecifier? SolveSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [ViewVariables]
    public List<EntityUid> Pieces = new();

    [ViewVariables]
    public List<int> Solution = new();

    [ViewVariables]
    public bool Finished;
}

[RegisterComponent]
public sealed partial class ADTConduitPieceComponent : Component
{
    [DataField]
    public int BaseMask = 1 | 4;

    [ViewVariables]
    public int Rotation;

    [ViewVariables]
    public EntityUid Source;

    [ViewVariables]
    public int Index;
}

[RegisterComponent]
public sealed partial class ADTGlyphPlatePuzzleComponent : Component
{
    [DataField(required: true)]
    public string Group = string.Empty;

    [DataField]
    public ProtoId<SourcePortPrototype>? SolvedPort = "ADTPuzzleSolved";

    [DataField]
    public LocId SolvedMessage = "helios-puzzle-plates-solved";

    [DataField]
    public SoundSpecifier? PlateSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField]
    public SoundSpecifier? SolveSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [ViewVariables]
    public bool Finished;

    [ViewVariables]
    public TimeSpan NextCheck;
}

[RegisterComponent]
public sealed partial class ADTGlyphPlateComponent : Component
{
    [DataField(required: true)]
    public string Group = string.Empty;

    [DataField(required: true)]
    public string Glyph = string.Empty;

    [ViewVariables]
    public bool Matched;
}

[RegisterComponent]
public sealed partial class ADTGlyphSampleComponent : Component
{
    [DataField(required: true)]
    public string Glyph = string.Empty;
}
