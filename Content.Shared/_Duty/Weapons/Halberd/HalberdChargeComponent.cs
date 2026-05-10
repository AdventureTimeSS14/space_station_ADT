using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Duty.Weapons.Halberd;

/// <summary>
/// Компонент рывка алебарды. Вешается на алебарду.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HalberdChargeComponent : Component
{
    // ── Action ────────────────────────────────────────────────

    [DataField]
    public EntProtoId ChargeActionId = "ActionDutyHalberdCharge";

    [DataField]
    public EntityUid? ChargeActionEntity;

    // ── Параметры способности ─────────────────────────────────

    /// <summary>Максимальная дистанция рывка в тайлах.</summary>
    [DataField]
    public float ChargeDistance = 12f;

    /// <summary>Скорость рывка (метров/сек).</summary>
    [DataField]
    public float ChargeSpeed = 18f;

    /// <summary>Урон при попадании в сущность (Slash).</summary>
    [DataField]
    public float ChargeDamage = 125f;

    /// <summary>Кулдаун способности.</summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(25);

    /// <summary>Резист ко всем типам урона во время рывка (0.0 - 1.0).</summary>
    [DataField]
    public float ChargeResistance = 0.65f;

    /// <summary>Стан при попадании в сущность (секунды).</summary>
    [DataField]
    public float KnockdownOnHitEntity = 5f;

    /// <summary>Стан при попадании в стену (секунды).</summary>
    [DataField]
    public float KnockdownOnHitWall = 8f;

    /// <summary>Стан при промахе (секунды).</summary>
    [DataField]
    public float KnockdownOnMiss = 2f;

    // ── Состояние рывка (рантайм) ─────────────────────────────

    public bool IsCharging = false;
    public EntityUid? ChargeUser = null;
    public System.Numerics.Vector2 ChargeDirection = System.Numerics.Vector2.Zero;
    public System.Numerics.Vector2 ChargeStartPos = System.Numerics.Vector2.Zero;
}

/// <summary>
/// Временный компонент-маркер, вешается на пользователя во время рывка.
/// Используется для применения резиста к урону.
/// </summary>
[RegisterComponent]
public sealed partial class HalberdChargeResistComponent : Component
{
    /// <summary>Доля урона которая блокируется (0.65 = 65% резист).</summary>
    public float Resistance = 0.65f;
}
