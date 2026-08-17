using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Salvage.Systems;

[RegisterComponent]
public sealed partial class ADTMegafaunaSpawnComponent : Component
{
    /// <summary>
    /// Сколько разных боссов заспавнить за раунд
    /// </summary>
    [DataField]
    public int Count = 5;

    [DataField]
    public float MinDistanceFromCenter = 100f;

    [DataField]
    public float MaxRadius = 220f;

    [DataField]
    public float MinSpacing = 70f;

    [DataField]
    public int MaxAttempts = 1000;

    [DataField]
    public bool AvoidRooms = true;

    [DataField]
    public float RoomClearance = 30f;

    [DataField]
    public List<EntProtoId> Bosses = new();

    [ViewVariables]
    public Vector2 BaseCenter = Vector2.Zero;
}
