using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Helios;

[RegisterComponent]
public sealed partial class ADTHeliosSecretsComponent : Component
{
    [ViewVariables]
    public Dictionary<string, string> Values = new();
}

[RegisterComponent]
public sealed partial class ADTHeliosSecretTextComponent : Component
{
    [DataField]
    public string? ExamineText;
}

[RegisterComponent]
public sealed partial class ADTHeliosCodeLockComponent : Component
{
    [DataField(required: true)]
    public string SecretKey = string.Empty;

    [ViewVariables]
    public string Typed = string.Empty;

    [ViewVariables]
    public bool Unlocked;
}

[RegisterComponent]
public sealed partial class ADTHeliosBreakerPanelComponent : Component
{
    [DataField]
    public string SecretKey = "BRK_MASK";

    [ViewVariables]
    public bool[] Lines = new bool[4];

    [ViewVariables]
    public bool Powered;
}

[RegisterComponent]
public sealed partial class ADTHeliosDualKeyComponent : Component
{
    [DataField(required: true)]
    public string Group = string.Empty;

    [DataField]
    public TimeSpan Window = TimeSpan.FromSeconds(1.5);

    [DataField]
    public ProtoId<SourcePortPrototype> SolvedPort = "ADTPuzzleSolved";

    [DataField]
    public SoundSpecifier? TurnSound = new SoundPathSpecifier("/Audio/Machines/door_lock_on.ogg");

    [DataField]
    public SoundSpecifier? SolveSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    [ViewVariables]
    public TimeSpan? TurnedAt;

    [ViewVariables]
    public bool Finished;
}

[RegisterComponent]
public sealed partial class ADTHeliosHoldPlateComponent : Component
{
    [DataField]
    public ProtoId<SourcePortPrototype> HeldPort = "ADTHeliosPlateHeld";

    [DataField]
    public ProtoId<SourcePortPrototype> ReleasedPort = "ADTHeliosPlateReleased";

    [DataField]
    public ProtoId<SinkPortPrototype> LockPort = "ADTHeliosPlateLock";

    [ViewVariables]
    public bool Locked;

    [DataField]
    public SoundSpecifier? PressSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [ViewVariables]
    public bool Held;

    [ViewVariables]
    public TimeSpan NextCheck;
}

[RegisterComponent]
public sealed partial class ADTHeliosReactionComponent : Component
{
    [DataField]
    public float Radius = 9f;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public bool Flicker = true;

    [DataField]
    public float Shake;

    [DataField]
    public List<EntProtoId> Spawns = new();

    [DataField]
    public bool OnStep;

    [DataField]
    public bool Once = true;

    [ViewVariables]
    public bool Fired;
}

[RegisterComponent, EntityCategory("Spawner")]
public sealed partial class ADTHeliosRandomSpotComponent : Component
{
    [DataField(required: true)]
    public string Group = string.Empty;

    [DataField(required: true)]
    public EntProtoId Proto;
}

[RegisterComponent]
public sealed partial class ADTHeliosRandomSpotsDoneComponent : Component
{
    [ViewVariables]
    public HashSet<string> Groups = new();

    [ViewVariables]
    public HashSet<(EntityUid Grid, Vector2i Tile)> UsedTiles = new();
}
