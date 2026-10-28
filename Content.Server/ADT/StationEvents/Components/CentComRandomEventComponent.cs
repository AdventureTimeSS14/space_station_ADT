using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Content.Server.ADT.StationEvents.Events;

namespace Content.Server.ADT.StationEvents.Components;

[RegisterComponent, Access(typeof(CentComRandomEvent))]
public sealed partial class CentComRandomEventComponent : Component
{
    [DataField("centComRandomEventPrototype")]
    public EntProtoId CentComRandomEventPrototype = "CentComRandomEvent";

    [DataField("wordSightingIds")]
    public List<int> WordSightingIds = new() { 3 };

    [DataField("weights")]
    public Dictionary<int, float> Weights { get; private set; } = new()
    {
        {1, 1f},
        {2, 1f},
        {3, 1f},
        {4, 1f},
        {5, 1f},
        {6, 1f},
        {7, 1f},
        {8, 1f}
    };

    [DataField]
    public Dictionary<int, float> RevolutionWeights = new()
    {
        {101, 2f},
        {102, 2f},
        {103, 2f},
        {104, 2f},
        {105, 2f},
        {106, 2f},
        {107, 2f},
        {108, 2f},
        {109, 2f},
        {110, 2f},
        {111, 2f},
        {112, 2f},
        {113, 2f},
        {114, 2f},
        {115, 2f},
        {116, 2f}
    };

    [DataField]
    public Dictionary<int, int> VariantCounts = new()
    {
        {3, 8},
        {4, 4},
        {6, 15},
        {7, 7},
        {8, 18},
        {101, 8},
        {106, 4}
    };

    [DataField]
    public int MaxBansPerRound = 15;
}
