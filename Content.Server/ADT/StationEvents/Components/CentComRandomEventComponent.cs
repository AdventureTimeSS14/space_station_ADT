using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.ADT.StationEvents.Events;

namespace Content.Server.ADT.StationEvents.Components;

[RegisterComponent, Access(typeof(CentComRandomEvent))]
public sealed partial class CentComRandomEventComponent : Component
{
    [DataField("centComRandomEventPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CentComRandomEventPrototype = "CentComRandomEvent";

    [DataField("wordSightingIds")]
    public List<int> WordSightingIds = new() { 3 };

    [DataField("weights")]
    public Dictionary<int, float> Weights { get; private set; } = new()
    {
        {1, 10f},
        {2, 6f},
        {3, 3f},
        {4, 9f},
        {5, 8f},
        {6, 8f},
        {7, 0.5f},
        {8, 6f},
        {9, 3f},
        {10, 0.5f},
        {11, 6f},
        {12, 5f},
        {13, 0.5f},
        {14, 8f},
        {15, 5f},
        {16, 0.5f}
    };
}
