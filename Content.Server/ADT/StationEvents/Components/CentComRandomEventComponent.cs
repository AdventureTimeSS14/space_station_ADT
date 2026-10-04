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
}
