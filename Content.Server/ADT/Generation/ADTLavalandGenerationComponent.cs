using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Generation;

[RegisterComponent]
public sealed partial class ADTLavalandGenerationComponent : Component
{
    [DataField]
    public float SafeRadius = 32f;

    [DataField]
    public float SafeEdgeNoise = 6f;

    [DataField]
    public float SafeEdgeFrequency = 0.05f;

    [DataField]
    public float SafeFalloff = 14f;

    [DataField]
    public float SafeFalloffFrequency = 0.16f;

    [DataField]
    public float MinRadius = 40f;

    [DataField]
    public float MaxRadius = 230f;

    [DataField]
    public int MaxAttempts = 1000;

    [DataField]
    public EntProtoId? BaseBeacon = "ADTGpsBeaconMiningBase";

    [DataField]
    public List<LavalandScatterGroup> Groups = new();

    [DataField]
    public int RiverNodes;

    [DataField]
    public float RiverMinRadius = 60f;

    [DataField]
    public float RiverMaxRadius = 200f;

    [DataField]
    public float RiverDetourChance = 0.2f;

    [DataField]
    public float RiverSpreadChance = 0.25f;

    [DataField]
    public float RiverSpreadLoss = 0.11f;

    [DataField]
    public float RiverRoomClearance = 20f;

    [DataField]
    public EntProtoId? RiverEntity;

    [DataField]
    public EntProtoId? RiverBridge;

    [DataField]
    public float RiverBridgeChance = 0.01f;

    [ViewVariables]
    public Vector2 BaseCenter = Vector2.Zero;

    [ViewVariables]
    public List<Vector2> Placed = new();

    [ViewVariables]
    public List<(Vector2 Center, float Radius)> Exclusions = new();
}

[DataDefinition]
public sealed partial class LavalandScatterGroup
{
    [DataField(required: true)]
    public List<EntProtoId> Prototypes = new();

    [DataField]
    public EntityWhitelist? RoomWhitelist;

    [DataField]
    public int Count = 1;

    [DataField]
    public float MinSpacing = 20f;

    [DataField]
    public float MinDistanceFromCenter = 40f;

    [DataField]
    public float? MaxDistanceFromCenter;

    [DataField]
    public float Clearance;

    [DataField]
    public bool AvoidRooms = true;
}
