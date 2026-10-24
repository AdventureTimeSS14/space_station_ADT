using System;
using System.Collections.Generic;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Shields;

/// <summary>Генератор щита: буфер энергии, поле, поглощение урона</summary>
[RegisterComponent]
public sealed partial class ShieldGeneratorComponent : Component
{
    [DataField] public EntProtoId SegmentProto = "ADTShieldSegment";
    [DataField] public EntProtoId ImpactProto = "ADTShieldImpact";

    [DataField] public ShieldRunningState Running = ShieldRunningState.Off;
    [DataField] public ShieldModes Modes = ShieldModes.None;

    [DataField] public float MitigationPhysical;
    [DataField] public float MitigationEm;
    [DataField] public float MitigationHeat;
    [DataField] public float MitigationMax = ShieldConstants.MaxMitigationBase;

    [DataField] public float OfflineFor;
    [DataField] public float Overloaded;
    [DataField] public bool EmergencyShutdown;
    [DataField] public float DischargeTimer;

    [DataField] public float InputCap = 1000000.0f;
    [DataField] public float MaxInputCap;
    [DataField] public float UpkeepMultiplier = 1f;
    [DataField] public float CurrentUpkeep;

    public const int RequiredConduitCount = 3;
    [DataField] public float InputPerConduit = 750000.0f;
    [DataField] public float MaxChargeBase = 100000000.0f;
    [DataField] public float ChargePerConduit = 25000000.0f;

    [DataField] public float DischargeDuration = 30f;
    [DataField] public float GracefulCooldown = 15f;
    [DataField] public float EmergencyCooldown = 120f;
    [DataField] public float UpdatePeriod = 0.5f;
    [DataField] public float StartCooldown = 3f;

    [DataField] public float OverchargeShockDamage = 25f;
    [DataField] public float OverchargeFieldStrain = 10f;

    [DataField] public float BypassStrainMin = 8f;
    [DataField] public float BypassStrainMax = 13f;

    [DataField] public float EmpChanceThreshold = 50f;
    [DataField] public float EmpChanceScale = 1.75f;
    [DataField] public float EmpPulseRadius = 7f;
    [DataField] public float EmpPulseEnergy = 50000.0f;
    [DataField] public float EmpPulseDuration = 30f;

/// <summary>Целостность поля (%), после которой перегрузка снимается и регенерация возобновляется.</summary>
    [DataField] public float OverloadClearIntegrity = 5f;

    public List<EntityUid> Conduits = new();
    public HashSet<EntityUid> Segments = new();
    public HashSet<Direction> LinkedDirections = new();

/// <summary>Единственный источник правды о поле: каждый тайл и его состояние. Сегменты ссылаются сюда по GridPos.</summary>
    public Dictionary<Vector2i, ShieldTileData> Tiles = new();

    public HashSet<Vector2i> DamagedTiles = new();

    [DataField] public bool Hacked;

    [DataField] public bool RequiresAnchor = true;

    [DataField] public bool RequiresConduits = true;

    [DataField] public HashSet<ProtoId<DamageContainerPrototype>> HumanoidContainers = new();

    [DataField] public HashSet<ProtoId<DamageContainerPrototype>> AnorganicContainers = new();

    public TimeSpan NextUpdate;
    public TimeSpan NextUiUpdate;

    public ShieldRunningState LastUiRunning;
    public bool LastUiOverloaded;
    public float LastUiIntegrity = float.NaN;
    public float LastUiEnergy = float.NaN;
    public float LastUiUpkeep = float.NaN;
    public float LastUiOffline = float.NaN;
    public int LastUiTotalSegments = -1;
    public int LastUiFunctionalSegments = -1;
    public int LastUiConduits = -1;
    public int LastUiModes;
}