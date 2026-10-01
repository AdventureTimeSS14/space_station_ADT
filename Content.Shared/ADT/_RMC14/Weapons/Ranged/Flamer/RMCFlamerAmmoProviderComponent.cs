// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class RMCFlamerAmmoProviderComponent : Component, IShootable
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "gun_magazine";

    /// <summary>
    ///     Задержка появления каждой следующей плитки в струе.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DelayPer = TimeSpan.FromSeconds(0.05);

    /// <summary>
    ///     Расход топлива на плитку. Также, чем выше, тем дольше горит плитка.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 CostPer = FixedPoint2.New(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan CantShootPopupLast;

    [DataField, AutoNetworkedField]
    public TimeSpan CantShootPopupCooldown = TimeSpan.FromSeconds(0.25);
}
