// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
// ReSharper disable CheckNamespace

using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reagent;

public sealed partial class ReagentPrototype
{
    /// <summary>
    ///     Сила пламени. Определяет урон и то, чью защиту от поджига огонь пробьёт.
    /// </summary>
    [DataField]
    public int Intensity;

    /// <summary>
    ///     Сколько секунд горит плитка и сколько стаков огня получает подожжённый.
    /// </summary>
    [DataField]
    public int Duration;

    /// <summary>
    ///     Максимальная дальность струи в плитках.
    /// </summary>
    [DataField]
    public int Radius;

    /// <summary>
    ///     Какую сущность огня спавнить на плитке.
    /// </summary>
    [DataField]
    public EntProtoId FireEntity = "RMCTileFire";

    /// <summary>
    ///     Прибавка к силе пламени за единицу реагента при крафте молотова.
    /// </summary>
    [DataField]
    public FixedPoint2 IntensityMod;

    /// <summary>
    ///     Прибавка ко времени горения за единицу давления в баке огнемёта.
    /// </summary>
    [DataField]
    public FixedPoint2 DurationMod;

    [DataField]
    public FixedPoint2 RadiusMod;

    [DataField]
    public bool FireSpread;

    [DataField]
    public bool BurnsInVacuum;

    [DataField]
    public TimeSpan VacuumBurnout = TimeSpan.FromSeconds(1.5);

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsFlammableReagent => Intensity > 0 && Duration > 0;
}
