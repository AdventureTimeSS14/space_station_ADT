using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Generation;

[RegisterComponent]
public sealed partial class ADTLavalandPopulationComponent : Component
{
    [DataField]
    public List<ADTLavalandPopulationGroup> Groups = new();

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public Vector2 BaseCenter = Vector2.Zero;

    [ViewVariables]
    public TimeSpan? PopulateAt;
}

[DataDefinition]
public sealed partial class ADTLavalandPopulationGroup
{
    [DataField(required: true)]
    public List<ADTLavalandPopulationEntry> Entries = new();

    [DataField]
    public int MinBudget = 1;

    [DataField]
    public int MaxBudget = 1;

    [DataField]
    public float MinSpacing = 12f;

    [DataField]
    public float MinDistanceFromCenter = 60f;

    [DataField]
    public float MaxRadius = 220f;

    [DataField]
    public bool AvoidRooms = true;

    [DataField]
    public float RoomClearance = 26f;

    [DataField]
    public int MaxAttempts = 80;

    [DataField]
    public int MaxFailures = 10;
}

[DataDefinition]
public sealed partial class ADTLavalandPopulationEntry
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public float Weight = 1f;

    [DataField]
    public int Cost = 1;

    [DataField]
    public List<ADTLavalandPopulationVariant> Variants = new();
}

[DataDefinition]
public sealed partial class ADTLavalandPopulationVariant
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public float Probability = 0.01f;
}
