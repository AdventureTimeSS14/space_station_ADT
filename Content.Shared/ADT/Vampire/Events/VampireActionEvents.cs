// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.Actions;
using Content.Shared.ADT.Damage;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Cloning;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Polymorph;
using Content.Shared.Projectiles;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Vampire;

/// <summary>
/// Interface for vampire action events that require blood costs.
/// Allows you to unify the verification and deduction of blood costs through the <see cref="BloodCost"/>.
/// </summary>
public interface IVampireActionEvent
{
    FixedPoint2 BloodCost { get; }
}

// Base
public sealed partial class VampireDrinkingBloodActionEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);
}

public sealed partial class VampireSelectClassActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireRejuvenateActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public bool Advanced = false;

    [DataField]
    public bool NoHeal = false;

    [DataField]
    public int Repeats = 5;

    [DataField]
    public DamageSpecifier Heal = default!;

    [DataField]
    public GroupHealSpecifier HealGroups = default!;

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(3.5);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireGlareActionEvent : EntityTargetActionEvent { }

// Diablerie
public sealed partial class VampireSacramentInitiationActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

// Hemomancer Abilities
public sealed partial class VampireClawsActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<StartingGearPrototype> ProtoId = "VampireClawsGear";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodTentacleAction : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId EntityId = "EffectBloodTentacleSpawn";

    [DataField]
    public List<Direction> OffsetDirections = new()
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest,
    };

    [DataField]
    public int ExtraSpawns = 8;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodBarrierActionEvent : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId EntityId = "BloodBarrier";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireSanguinePoolActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphProto = "VampireBlood";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampirePredatorSensesActionEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId EntityId = "PuddleBlood";

    [DataField]
    public SoundSpecifier Sound;
}

public sealed partial class VampireBloodEruptionActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodBringersRiteActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<AlertPrototype> Alert = "AlertBloodRite";

    [DataField]
    public DamageSpecifier Heal = default!;

    [DataField]
    public GroupHealSpecifier HealGroups = default!;

    [DataField]
    public float StaminaMod = -15f;

    [DataField]
    public SoundSpecifier Sound;

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(1);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

// Umbrae Abilities
public sealed partial class VampireCloakOfDarknessActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public float SpeedMod = 1.3f;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireShadowSnareActionEvent : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId EntityId = "ShadowTrap";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireSoulAnchorActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireDarkPassageActionEvent : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId MistEffect = "VampireMistEffect";

    [DataField]
    public EntProtoId MistReappearEffect = "VampireMistReappearEffect";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireExtinguishActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireShadowBoxingActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId EntityId = "MobFollowerShadow";

    [DataField]
    public int Repeats = 10;

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(1);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireEternalDarknessActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<AlertPrototype> Alert = "AlertEternalDarkness";

    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(1);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

// Gargantua Abilities
public sealed partial class VampireBloodSwellActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(30);

    [DataField]
    public bool Advanced = false;

    [DataField]
    public ProtoId<DamageTypePrototype> BonusDamageType = "Blunt";

    [DataField]
    public float AttackRate = 2;

    [DataField]
    public float BonusDamageAmount = 20f;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodRushActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(10);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireSeismicStompActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public SoundSpecifier Sound;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireOverwhelmingForceActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireDemonicGraspActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireChargeActionEvent : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField("components")]
    public ComponentRegistry EnsurableComponents;

    [DataField]
    public SoundSpecifier Sound;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

// Dantalion Abilities
public sealed partial class VampireEnthrallActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireCommuneActionEvent : InstantActionEvent { }

public sealed partial class VampirePacifyActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(20);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireSubspaceSwapActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireDeployDecoyActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField("components")]
    public ComponentRegistry EnsurableComponents;

    [DataField]
    public ProtoId<CloningSettingsPrototype> Settings = "BaseClone";

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(6);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireRallyThrallsActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBloodBondActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(1);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireMassHysteriaActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireThrallHealActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public int Repeats = 3;

    [DataField]
    public DamageSpecifier Heal = default!;

    [DataField]
    public GroupHealSpecifier HealGroups = default!;

    [DataField]
    public TimeSpan TimeInterval = TimeSpan.FromSeconds(4);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampirePacifyNearbyActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(8);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

// Bestia Abilities
public sealed partial class VampireCheckTrophiesActionEvent : InstantActionEvent { }

public sealed partial class VampireDissectActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireInfectedTrophyActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId<ProjectileComponent> ProjectileId = "ProjectileInfectedTrophy";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireLungeActionEvent : WorldTargetActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireMarkPreyActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Heat";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireMetamorphosisBatsActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphProto = "VampireBats";

    [DataField]
    public ProtoId<DamageTypePrototype> BonusDamageType = "Piercing";

    [DataField]
    public EntProtoId MistEffect = "VampireMistEffect";

    [DataField]
    public EntProtoId MistReappearEffect = "VampireMistReappearEffect";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireAnabiosisActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId CoffinProto = "CrateCoffinVampire";

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireSummonBatsActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public EntProtoId BatsProto = "MobBats";

    [DataField]
    public ProtoId<DamageTypePrototype> BonusDamageType = "Piercing";

    [DataField]
    public SoundSpecifier Sound;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireMetamorphosisHoundActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> PolymorphProto = "VampireHound";

    [DataField]
    public ProtoId<DamageTypePrototype> BonusDamageType = "Piercing";

    [DataField]
    public EntProtoId MistEffect = "VampireMistEffect";

    [DataField]
    public EntProtoId MistReappearEffect = "VampireMistReappearEffect";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

// Polymorph Abilities
public sealed partial class VampireResonantShriekActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField]
    public SoundSpecifier Sound;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireLungeFinaleActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

// Thrall Abilities
public sealed partial class VampireThrallCommuneActionEvent : InstantActionEvent { }

// Variant Abilities
public sealed partial class VampireGoonDiseaseActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public ProtoId<ADTDiseasePrototype> Disease = "VampireGraveFever";

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireBlinkActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public float InnerRadius = 2f;

    [DataField]
    public float OuterRadius = 6f;

    [DataField]
    public int Attempts = 12;

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireHypnotiseActionEvent : EntityTargetActionEvent, IVampireActionEvent
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan SleepTime = TimeSpan.FromSeconds(40);

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}

public sealed partial class VampireScreechActionEvent : InstantActionEvent, IVampireActionEvent
{
    [DataField]
    public float Range = 4f;

    [DataField]
    public float StaminaDamage = 60f;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan StutterTime = TimeSpan.FromSeconds(40);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/ADT/Vampire/Effects/Vampire/creepyshriek.ogg");

    [DataField] public FixedPoint2 BloodCost { get; private set; }
}
