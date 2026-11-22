using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Salvage.Systems;

[RegisterComponent]
public sealed partial class ADTMegafaunaPendingChargeComponent : Component
{
    [ViewVariables]
    public List<ADTMegafaunaPendingCharge> Queue = new();
}

public sealed class ADTMegafaunaChargeOptions
{
    public float Overshoot = 1.5f;

    public float MinDistance = 3f;

    public Color? TrailColor;

    public bool LeadTarget = true;

    public bool SmashPath = true;

    public bool PassThroughMobs;

    public float TrampleRadius = 1.1f;

    public float ArmorPenetration;

    public float? TelegraphLifetime;
}

public sealed class ADTMegafaunaPendingCharge
{
    public TimeSpan TelegraphAt;

    public TimeSpan ExecuteAt;

    public bool TelegraphSpawned;

    public bool Scheduled = true;

    public float Delay;

    public MapCoordinates Target;

    public EntityUid? TargetEntity;

    public float Speed = 8f;

    public float TrampleDamage = 30f;

    public bool ExpireOnHit;

    public ADTMegafaunaChargeOptions Options = new();

    public EntProtoId TelegraphProto = "ADTBubblegumChargeTelegraph";
}

[RegisterComponent]
public sealed partial class ADTMegafaunaActiveChargeComponent : Component
{
    [ViewVariables]
    public TimeSpan EndsAt;

    [ViewVariables]
    public MapCoordinates TargetCoords;

    [ViewVariables]
    public Vector2 Direction;

    [ViewVariables]
    public float TrampleDamage = 30f;

    [ViewVariables]
    public bool ExpireOnHit;

    [ViewVariables]
    public ADTMegafaunaChargeOptions Options = new();

    [DataField]
    public float SmashBlunt = 200f;

    [DataField]
    public float SmashStructural = 300f;

    [DataField]
    public float CameraKickStrength = 0.6f;

    [ViewVariables]
    public HashSet<EntityUid> AlreadySmashed = new();

    [ViewVariables]
    public HashSet<EntityUid> AlreadyHit = new();
}

public enum ADTMegafaunaChargeEndReason : byte
{
    Landed,
    Expired,
    Hit,
    Failed,
}

[ByRefEvent]
public record struct ADTMegafaunaChargeTelegraphEvent(MapCoordinates Target, EntityUid? TargetEntity, bool Handled = false);

[ByRefEvent]
public record struct ADTMegafaunaChargeTrampleAttemptEvent(EntityUid Charger, bool Cancelled = false);

[ByRefEvent]
public readonly record struct ADTMegafaunaChargeTrampledEvent(EntityUid Target, Vector2 Direction);

[ByRefEvent]
public readonly record struct ADTMegafaunaChargeEndedEvent(ADTMegafaunaChargeEndReason Reason);

[ByRefEvent]
public record struct ADTMegafaunaBusyCheckEvent(bool Busy = false);
