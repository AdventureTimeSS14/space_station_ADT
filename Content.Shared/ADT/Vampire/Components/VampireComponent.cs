// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Metabolism;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Vampire.Components;

/// <summary>
/// The basic component that defines a vampire.
/// </summary>
[Access(typeof(SharedVampireSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VampireComponent : Component
{
    public static readonly ProtoId<DamageModifierSetPrototype> VampireDamageModifier = "Vampire";
    public static readonly ProtoId<MetabolizerTypePrototype> MetabolizerVampire = "Vampire";

    public static readonly EntProtoId DrinkActionPrototype = "ActionDrinkBlood";
    public static readonly EntProtoId SelectClassActionPrototype = "ActionVampireSelectClass";
    public static readonly EntProtoId RejuvenateActionPrototype = "ActionVampireRejuvenate";
    public static readonly EntProtoId GlareActionPrototype = "ActionVampireGlare";

    public EntityUid? DrinkActionEntity;
    public EntityUid? SelectClassActionEntity;
    public EntityUid? RejuvenateActionEntity;
    public EntityUid? GlareActionEntity;

    [ViewVariables(VVAccess.ReadOnly)]
    public float NextSpaceDamageTick { get; set; }

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public VampireClassEnum CurrentEvolution { get; set; } = VampireClassEnum.NonSelected;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public VampireVariant Variant = VampireVariant.Classic;

    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> DrainedVictims = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, TimeSpan> BaseActionDelays = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public bool CloakActive;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool CloakInDarkness;

    [ViewVariables(VVAccess.ReadOnly)]
    public float CloakWalkSpeed;

    [ViewVariables(VVAccess.ReadOnly)]
    public float CloakSprintSpeed;

    [ViewVariables(VVAccess.ReadOnly)]
    public float NextCloakCheck;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 CurrentBlood = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public float TotalBloodDrank = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, FixedPoint2> BloodConsumedFromVictim = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntProtoId, EntityUid?> AcquiredSkills = new();

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<VampireClassEnum, Dictionary<float, List<EntProtoId>>> ClassThresholds { get; set; } = new()
    {
        [VampireClassEnum.Hemomancer] = new()
        {
            { 150f, new() { "ActionVampireClaws" } },
            { 250f, new() { "ActionVampireBloodTendrils", "ActionVampireBloodBarrier" } },
            { 400f, new() { "ActionVampireSanguinePool" } },
            { 600f, new() { "ActionVampirePredatorSenses" } },
            { 800f, new() { "ActionVampireBloodEruption" } },
            { 1000f, new() { "ActionVampireBloodBringersRite" } }
        },
        [VampireClassEnum.Umbrae] = new()
        {
            { 150f, new() { "ActionVampireCloakOfDarkness" } },
            { 250f, new() { "ActionVampireShadowSnare", "ActionVampireSoulAnchor" } },
            { 400f, new() { "ActionVampireDarkPassage" } },
            { 600f, new() { "ActionVampireExtinguish" } },
            { 800f, new() { "ActionVampireShadowBoxing" } },
            { 1000f, new() { "ActionVampireEternalDarkness" } }
        },
        [VampireClassEnum.Gargantua] = new()
        {
            { 150f, new() { "ActionVampireBloodSwell" } },
            { 250f, new() { "ActionVampireBloodRush", "ActionVampireSeismicStomp" } },
            { 400f, new() { "ActionVampireBloodSwellAdvanced" } },
            { 600f, new() { "ActionVampireOverwhelmingForce" } },
            { 800f, new() { "ActionDemonicGrasp" } },
            { 1000f, new() { "ActionVampireCharge" } }
        },
        [VampireClassEnum.Dantalion] = new()
        {
            { 150f, new() { "ActionEnthrall", "ActionCommune" } },
            { 250f, new() { "ActionPacify", "ActionSubspaceSwap" } },
            { 400f, new() { "ActionDeployDecoy" } },
            { 600f, new() { "ActionRallyThralls", "ActionVampirePacifyNearby" } },
            { 800f, new() { "ActionBloodBond", "ActionVampireThrallHeal" } },
            { 1000f, new() { "ActionMassHysteria" } }
        },
        [VampireClassEnum.Bestia] = new()
        {
            { 150f, new() { "ActionVampireCheckTrophies", "ActionVampireDissect", "ActionVampireInfectedTrophy" } },
            { 250f, new() { "ActionVampireLunge", "ActionVampireMarkPrey" } },
            { 400f, new() { "ActionVampireMetamorphosisBats" } },
            { 600f, new() { "ActionVampireAnabiosis" } },
            { 800f, new() { "ActionVampireSummonBats" } },
            { 1000f, new() { "ActionVampireMetamorphosisHound" } }
        },
        [VampireClassEnum.Ancient] = new()
        {
            { 0f, new()
                {
                    "ActionVampireClaws", "ActionVampireBloodTendrils", "ActionVampireBloodBarrier",
                    "ActionVampireSanguinePool", "ActionVampirePredatorSenses", "ActionVampireBloodEruption",
                    "ActionVampireBloodBringersRite",
                    "ActionVampireCloakOfDarkness", "ActionVampireShadowSnare", "ActionVampireSoulAnchor",
                    "ActionVampireDarkPassage", "ActionVampireExtinguish", "ActionVampireShadowBoxing",
                    "ActionVampireEternalDarkness",
                    "ActionVampireBloodSwell", "ActionVampireBloodRush", "ActionVampireSeismicStomp",
                    "ActionVampireBloodSwellAdvanced", "ActionVampireOverwhelmingForce", "ActionDemonicGrasp",
                    "ActionVampireCharge",
                    "ActionEnthrall", "ActionCommune", "ActionPacify", "ActionSubspaceSwap", "ActionDeployDecoy",
                    "ActionRallyThralls", "ActionVampirePacifyNearby", "ActionBloodBond", "ActionVampireThrallHeal",
                    "ActionMassHysteria",
                    "ActionVampireCheckTrophies", "ActionVampireDissect", "ActionVampireInfectedTrophy",
                    "ActionVampireLunge", "ActionVampireMarkPrey", "ActionVampireMetamorphosisBats",
                    "ActionVampireAnabiosis", "ActionVampireSummonBats", "ActionVampireMetamorphosisHound"
                }
            }
        }
    };

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<VampireVariant, Dictionary<float, List<EntProtoId>>> VariantThresholds { get; set; } = new()
    {
        [VampireVariant.Goon] = new()
        {
            { 0f, new() { "ActionVampireHypnotise" } },
            { 100f, new() { "ActionVampireMetamorphosisBats" } },
            { 150f, new() { "ActionVampireCloakOfDarkness", "ActionVampireGoonDisease" } },
            { 200f, new() { "ActionVampireSummonBats", "ActionVampireScreech" } },
            { 250f, new() { "ActionVampireBlink" } },
            { 300f, new() { "ActionPolymorphJaunt", "ActionEnthrall" } }
        },
        [VampireVariant.Devil] = new()
        {
            { 100f, new() { "ActionVampireMetamorphosisBats" } },
            { 150f, new() { "ActionVampireCloakOfDarkness", "ActionVampireShadowSnare" } },
            { 200f, new() { "ActionVampireExtinguish", "ActionVampireScreech" } },
            { 250f, new() { "ActionVampireBlink" } },
            { 300f, new() { "ActionPolymorphJaunt" } }
        }
    };

    [DataField]
    public ProtoId<AlertPrototype> BloodAlert = "BloodAlert";

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "VampireFaction";

    [DataField]
    public DamageSpecifier HolyDamage = new()
    {
        DamageDict = { { "Heat", 10 } }
    };

    [DataField]
    public DamageSpecifier SpaceDamage = new()
    {
        DamageDict = { { "Heat", 2.5 } }
    };

    [DataField]
    public SoundSpecifier BloodDrainSound = new SoundPathSpecifier("/Audio/Items/drink.ogg",
        new AudioParams { Volume = -3f, MaxDistance = 3f }
    );
}
