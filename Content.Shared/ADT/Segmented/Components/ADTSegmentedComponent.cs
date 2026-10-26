using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Segmented.Components;

[RegisterComponent]
public sealed partial class ADTSegmentedComponent : Component
{
    [DataField(required: true)]
    public List<ADTSegmentEntry> Segments = new();

    [DataField]
    public bool DeleteOnDeath;

    [DataField]
    public float TeleportDistance = 3f;

    [ViewVariables]
    public List<EntityUid?> Spawned = new();

    [ViewVariables]
    public Vector2 MoveDirection = new(0f, -1f);

    [ViewVariables]
    public Dictionary<int, Vector2> AnchorOverrides = new();
}

[DataDefinition]
public sealed partial class ADTSegmentEntry
{
    [DataField(required: true)]
    public EntProtoId Proto;

    [DataField]
    public ADTSegmentMode Mode = ADTSegmentMode.Step;

    [DataField]
    public Vector2 Offset;

    [DataField]
    public bool RotateWithRoot;

    [DataField]
    public int Group;

    [DataField]
    public float StepThreshold = 1f;

    [DataField]
    public float StepLead = 0.5f;

    [DataField]
    public TimeSpan StepDuration = TimeSpan.FromSeconds(0.3);

    [DataField]
    public float StepHeight = 0.25f;

    [DataField]
    public float Spacing = 1f;

    [DataField]
    public float LeashDistance = 4f;
}

public enum ADTSegmentMode : byte
{
    Step,
    Chain,
    Fixed,
}
