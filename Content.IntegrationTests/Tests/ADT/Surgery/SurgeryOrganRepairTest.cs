using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.ADT.Body;
using Content.Server.Atmos.Components;
using Content.Shared.ADT.Body.Legs;
using Content.Shared.ADT.Body.Limbs;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.VoiceOfGod;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes.Conditions;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.Emp;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;
using SharedDoAfter = Content.Shared.DoAfter.DoAfter;

namespace Content.IntegrationTests.Tests.ADT.Surgery;

[TestFixture]
public sealed class SurgeryOrganRepairTest : GameTest
{
    private const string ChestGraph = "SurgeryChestOrgans";

    [SidedDependency(Side.Server)] private StatusEffectsSystem _status = default!;
    [SidedDependency(Side.Server)] private OrganHealthSystem _organHealth = default!;
    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private SharedEmpSystem _emp = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void OrganicHeartIsRepairedBySurgery()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();
        var comp = StartGraph(patient, "ChestOpen");

        Assert.That(_organHealth.TryGetOrgan(patient, "Heart", out var heart), Is.True);
        _organHealth.SetDamage((heart.Owner, heart.Comp), 30f);
        Assert.That(heart.Comp.Stage, Is.EqualTo(OrganStage.Damaged));

        RaiseStep(patient, surgeon, "RepairHeart", 0, SSpawn("Hemostat"));
        RaiseStep(patient, surgeon, "RepairHeart", 1, SSpawn("Cautery"));

        Assert.That(heart.Comp.Damage, Is.LessThanOrEqualTo(heart.Comp.MaxDamage * 0.2f));
        Assert.That(OrganHealthSystem.CanRegenerate(heart.Comp), Is.True);
        Assert.That(heart.Comp.Operated, Is.True);
        Assert.That(comp.CurrentNode, Is.EqualTo("ChestOpen"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void OrganRegeneratesOnlyAboveEightyPercentHealth()
    {
        var patient = SSpawn("MobHuman");
        Assert.That(_organHealth.TryGetOrgan(patient, "Liver", out var liver), Is.True);
        var max = liver.Comp.MaxDamage;

        _organHealth.SetDamage((liver.Owner, liver.Comp), max * 0.2f);
        Assert.That(OrganHealthSystem.CanRegenerate(liver.Comp), Is.True);

        _organHealth.SetDamage((liver.Owner, liver.Comp), max * 0.25f);
        Assert.That(OrganHealthSystem.CanRegenerate(liver.Comp), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void BrainIsDamagedAndRepairedBySurgery()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();
        var comp = SComp<OperatedComponent>(patient);
        comp.GraphId = "SurgeryHeadOrgans";
        comp.CurrentNode = "SkullOpen";

        Assert.That(_organHealth.TryGetOrgan(patient, "Brain", out var brain), Is.True);
        _organHealth.SetDamage((brain.Owner, brain.Comp), brain.Comp.MaxDamage);
        Assert.That(brain.Comp.Stage, Is.EqualTo(OrganStage.Failing));

        RaiseStep(patient, surgeon, "RepairBrain", 0, SSpawn("Scalpel"));
        RaiseStep(patient, surgeon, "RepairBrain", 1, SSpawn("Hemostat"));

        Assert.That(OrganHealthSystem.CanRegenerate(brain.Comp), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void TendOperationsHealTheirOwnDamageGroupOnCorpse()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();
        var comp = StartGraph(patient, "AbdomenOpen");
        comp.GraphId = "SurgeryAbdomenOrgans";

        var damageable = SEntMan.System<DamageableSystem>();
        var mobState = SEntMan.System<MobStateSystem>();
        var wounds = new DamageSpecifier { DamageDict = new() { { "Blunt", 120 }, { "Heat", 120 } } };
        damageable.TryChangeDamage(patient, wounds, ignoreResistances: true);
        Assert.That(mobState.IsDead(patient), Is.True);

        var brute = damageable.GetDamagePerGroup(patient)["Brute"];
        var burn = damageable.GetDamagePerGroup(patient)["Burn"];

        RaiseStep(patient, surgeon, "TendBrute", 0, SSpawn("Hemostat"));
        RaiseStep(patient, surgeon, "TendBrute", 1, SSpawn("Cautery"));

        Assert.That(damageable.GetDamagePerGroup(patient)["Brute"], Is.LessThan(brute));
        Assert.That(damageable.GetDamagePerGroup(patient)["Burn"], Is.EqualTo(burn));

        RaiseStep(patient, surgeon, "TendBurn", 0, SSpawn("Scalpel"));
        RaiseStep(patient, surgeon, "TendBurn", 1, SSpawn("Hemostat"));

        Assert.That(damageable.GetDamagePerGroup(patient)["Burn"], Is.LessThan(burn));
        Assert.That(mobState.IsDead(patient), Is.True, "healing alone must not revive a corpse");
        Assert.That(comp.CurrentNode, Is.EqualTo("AbdomenOpen"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void BarotraumaLeavesLungsTenPercentButNotEars()
    {
        var patient = SSpawn("MobHuman");
        var trauma = SEntMan.System<OrganTraumaSystem>();
        var comp = SComp<OrganTraumaComponent>(patient);
        SComp<BarotraumaComponent>(patient).TakingDamage = true;

        Assert.That(_organHealth.TryGetOrgan(patient, "Lungs", out var lungs), Is.True);
        Assert.That(_organHealth.TryGetOrgan(patient, "Ears", out var ears), Is.True);

        for (var i = 0; i < 200; i++)
        {
            trauma.UpdateBarotrauma((patient, comp));
        }

        Assert.That(lungs.Comp.Damage, Is.EqualTo(lungs.Comp.MaxDamage * 0.9f).Within(0.001f));
        Assert.That(lungs.Comp.Stage, Is.EqualTo(OrganStage.Damaged));
        Assert.That(ears.Comp.Stage, Is.EqualTo(OrganStage.Failing));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void DefibrillatorIsExcludedFromElectrocutionHeartTrauma()
    {
        var patient = SSpawn("MobHuman");
        Assert.That(_organHealth.TryGetOrgan(patient, "Heart", out var heart), Is.True);

        SEntMan.EventBus.RaiseLocalEvent(patient, new ElectrocutedEvent(patient, SSpawn("Defibrillator"), 1f));
        Assert.That(heart.Comp.Damage, Is.Zero);

        SEntMan.EventBus.RaiseLocalEvent(patient, new ElectrocutedEvent(patient, null, 1f));
        Assert.That(heart.Comp.Damage, Is.GreaterThan(0f));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RepairConditionsMatchOrganKindAndDamage()
    {
        var patient = SSpawn("MobHuman");
        Assert.That(_organHealth.TryGetOrgan(patient, "Heart", out var heart), Is.True);

        var damaged = new OrganDamagedCondition { OrganId = "Heart", MinStage = OrganStage.Bruised };
        var organic = new OrganKindCondition { OrganId = "Heart", Kind = OrganKind.Organic };
        var robotic = new OrganKindCondition { OrganId = "Heart", Kind = OrganKind.Robotic };

        Assert.That(damaged.Check(patient, SEntMan), Is.False);
        _organHealth.SetDamage((heart.Owner, heart.Comp), 30f);
        Assert.That(damaged.Check(patient, SEntMan), Is.True);
        Assert.That(organic.Check(patient, SEntMan), Is.True);
        Assert.That(robotic.Check(patient, SEntMan), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void EmpMakesCyberHeartDecayAndMechanicRepairStopsIt()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();
        var comp = StartGraph(patient, "ChestOpen");

        Assert.That(_organHealth.TryGetOrgan(patient, "Heart", out var oldHeart), Is.True);
        Assert.That(_body.TryRemoveOrgan(oldHeart.Owner), Is.True);
        Assert.That(_body.TryInsertOrgan(patient, SSpawn("ADTOrganCyberHeart")), Is.True);
        Assert.That(_organHealth.TryGetOrgan(patient, "Heart", out var heart), Is.True);
        Assert.That(heart.Comp.Kind, Is.EqualTo(OrganKind.Robotic));
        Assert.That(heart.Comp.MaxDamage, Is.EqualTo(37.5f));

        var cyber = SComp<CyberOrganComponent>(heart.Owner);
        cyber.EmpFailureChance = 1f;

        _emp.DoEmpEffects(patient, 1f, TimeSpan.FromSeconds(5));
        Assert.That(cyber.Decaying, Is.True);

        RaiseStep(patient, surgeon, "RepairHeart", 0, SSpawn("Hemostat"));
        Assert.That(cyber.Decaying, Is.True, "organic repair must not touch a cybernetic organ");

        RaiseStep(patient, surgeon, "RepairHeartMechanic", 0, SSpawn("Screwdriver"));
        RaiseStep(patient, surgeon, "RepairHeartMechanic", 1, SSpawn("Multitool"));

        Assert.That(cyber.Decaying, Is.False);
        Assert.That(comp.CurrentNode, Is.EqualTo("ChestOpen"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ImplantIsExtractedBySurgery()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();
        StartGraph(patient, "ChestSkinRetracted");

        var condition = new HasImplantCondition();
        Assert.That(condition.Check(patient, SEntMan), Is.False);

        Assert.That(SEntMan.System<SharedSubdermalImplantSystem>().AddImplant(patient, "TrackingImplant"), Is.Not.Null);
        Assert.That(condition.Check(patient, SEntMan), Is.True);

        RaiseStep(patient, surgeon, "ExtractImplant:TrackingImplant", 0, SSpawn("Hemostat"));

        Assert.That(condition.Check(patient, SEntMan), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void PermanentImplantIsNotExtracted()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();
        StartGraph(patient, "ChestSkinRetracted");

        var implant = SEntMan.System<SharedSubdermalImplantSystem>().AddImplant(patient, "MicroBombImplant");
        Assert.That(implant, Is.Not.Null);
        Assert.That(new HasImplantCondition().Check(patient, SEntMan), Is.False);

        RaiseStep(patient, surgeon, "ExtractImplant:MicroBombImplant", 0, SSpawn("Hemostat"));

        Assert.That(SEntMan.GetComponent<ImplantedComponent>(patient).ImplantContainer.Contains(implant!.Value), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CursedHeartIsInsertedBySurgery()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();
        StartGraph(patient, "ChestOpen");

        RaiseStep(patient, surgeon, "RemoveHeart", 0, SSpawn("Hemostat"));
        Assert.That(_organHealth.TryGetOrgan(patient, "Heart", out _), Is.False);

        RaiseStep(patient, surgeon, "InsertHeart", 0, SSpawn("ADTCursedHeart"));

        Assert.That(SEntMan.HasComponent<CursedHeartComponent>(patient), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void VocalCordsAreInsertedAndRemovedBySurgery()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();
        var comp = SComp<OperatedComponent>(patient);
        comp.GraphId = "SurgeryHeadOrgans";
        comp.CurrentNode = "MouthOpen";

        RaiseStep(patient, surgeon, "InsertVocalCords", 0, SSpawn("ADTOrganVoiceOfGod"));
        Assert.That(SEntMan.HasComponent<ADTVoiceOfGodSpeakerComponent>(patient), Is.True);

        RaiseStep(patient, surgeon, "RemoveVocalCords", 0, SSpawn("Hemostat"));
        Assert.That(SEntMan.HasComponent<ADTVoiceOfGodSpeakerComponent>(patient), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MissingLegsSlowDownAndPreventStanding()
    {
        var patient = SSpawn("MobHuman");
        var detachable = SEntMan.System<DetachableOrganSystem>();
        var movement = SComp<MovementSpeedModifierComponent>(patient);
        var standing = SEntMan.System<StandingStateSystem>();

        Assert.That(movement.CurrentWalkSpeed, Is.EqualTo(movement.BaseWalkSpeed).Within(0.001f));

        DetachOrgan(patient, "LegLeft", detachable);
        Assert.That(movement.CurrentWalkSpeed, Is.EqualTo(movement.BaseWalkSpeed * 0.5f).Within(0.001f));
        Assert.That(standing.IsDown(patient), Is.False);

        DetachOrgan(patient, "LegRight", detachable);
        Assert.That(standing.IsDown(patient), Is.True);
        Assert.That(standing.Stand(patient), Is.False);
        Assert.That(movement.CurrentWalkSpeed, Is.LessThan(movement.BaseWalkSpeed));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MissingFeetSlowDown()
    {
        var patient = SSpawn("MobHuman");
        var detachable = SEntMan.System<DetachableOrganSystem>();
        var movement = SComp<MovementSpeedModifierComponent>(patient);
        var legs = SComp<LegMobilityComponent>(patient);
        var standing = SEntMan.System<StandingStateSystem>();

        DetachOrgan(patient, "FootLeft", detachable);
        Assert.That(movement.CurrentWalkSpeed, Is.EqualTo(movement.BaseWalkSpeed * legs.MissingFootModifier).Within(0.001f));
        Assert.That(movement.CurrentSprintSpeed, Is.EqualTo(movement.BaseSprintSpeed * legs.MissingFootModifier).Within(0.001f));

        DetachOrgan(patient, "FootRight", detachable);
        Assert.That(movement.CurrentWalkSpeed, Is.EqualTo(movement.BaseWalkSpeed * legs.MissingFootModifier * legs.MissingFootModifier).Within(0.001f));
        Assert.That(standing.IsDown(patient), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void LosingAllLimbsOfPairDropsClothingAndBlocksIt()
    {
        var patient = SSpawn("MobHuman");
        var detachable = SEntMan.System<DetachableOrganSystem>();
        var inventory = SEntMan.System<InventorySystem>();
        var shoes = SSpawn("ClothingShoesColorBlack");
        var gloves = SSpawn("ClothingHandsGlovesColorBlack");

        Assert.That(inventory.TryEquip(patient, shoes, "shoes", silent: true, force: true), Is.True);
        Assert.That(inventory.TryEquip(patient, gloves, "gloves", silent: true, force: true), Is.True);

        DetachOrgan(patient, "LegLeft", detachable);
        DetachOrgan(patient, "ArmRight", detachable);
        Assert.That(inventory.TryGetSlotEntity(patient, "shoes", out _), Is.True);
        Assert.That(inventory.TryGetSlotEntity(patient, "gloves", out _), Is.True);

        var loss = SComp<LimbLossComponent>(patient);
        Assert.That(loss.Missing, Is.EqualTo(LimbParts.LegLeft | LimbParts.FootLeft | LimbParts.ArmRight | LimbParts.HandRight));

        DetachOrgan(patient, "FootRight", detachable);
        DetachOrgan(patient, "ArmLeft", detachable);
        Assert.That(inventory.TryGetSlotEntity(patient, "shoes", out _), Is.False);
        Assert.That(inventory.TryGetSlotEntity(patient, "gloves", out _), Is.False);

        Assert.That(inventory.CanEquip(patient, shoes, "shoes", out var shoesReason), Is.False);
        Assert.That(shoesReason, Is.EqualTo("limb-loss-cannot-equip-shoes"));
        Assert.That(inventory.CanEquip(patient, gloves, "gloves", out var glovesReason), Is.False);
        Assert.That(glovesReason, Is.EqualTo("limb-loss-cannot-equip-gloves"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ReattachedFootClearsLimbLoss()
    {
        var patient = SSpawn("MobHuman");
        var detachable = SEntMan.System<DetachableOrganSystem>();
        var inventory = SEntMan.System<InventorySystem>();
        var shoes = SSpawn("ClothingShoesColorBlack");
        var foot = FindOrgan(patient, "FootLeft");
        var leg = FindOrgan(patient, "LegLeft");

        detachable.Detach(foot);
        DetachOrgan(patient, "FootRight", detachable);
        Assert.That(SComp<LimbLossComponent>(patient).Missing, Is.EqualTo(LimbParts.FootLeft | LimbParts.FootRight));

        inventory.CanEquip(patient, shoes, "shoes", out var blockedReason);
        Assert.That(blockedReason, Is.EqualTo("limb-loss-cannot-equip-shoes"));

        Assert.That(_body.TryInsertOrgan(patient, foot, leg), Is.True);
        Assert.That(SComp<LimbLossComponent>(patient).Missing, Is.EqualTo(LimbParts.FootRight));

        inventory.CanEquip(patient, shoes, "shoes", out var reason);
        Assert.That(reason, Is.Not.EqualTo("limb-loss-cannot-equip-shoes"));
    }

    private EntityUid FindOrgan(EntityUid patient, string category)
    {
        foreach (var organ in _body.EnumerateOrgans<OrganComponent>(patient))
        {
            if (organ.Comp1.Category == category)
                return organ.Owner;
        }

        Assert.Fail($"{category} not found");
        return default;
    }

    private void DetachOrgan(EntityUid patient, string category, DetachableOrganSystem detachable)
    {
        foreach (var organ in _body.EnumerateOrgans<OrganComponent>(patient))
        {
            if (organ.Comp1.Category != category)
                continue;

            detachable.Detach(organ.Owner);
            return;
        }

        Assert.Fail($"{category} not found");
    }

    private EntityUid SpawnSurgeon()
    {
        var surgeon = SSpawn("MobHuman");
        SEntMan.EnsureComponent<SurgerySkillComponent>(surgeon);
        return surgeon;
    }

    private EntityUid SpawnPatient()
    {
        var patient = SSpawn("MobHuman");
        SEntMan.EnsureComponent<OperatedComponent>(patient);
        _status.TryAddStatusEffectDuration(patient, "StatusEffectAnesthetized", TimeSpan.FromMinutes(30));
        return patient;
    }

    private OperatedComponent StartGraph(EntityUid patient, string node)
    {
        var comp = SEntMan.EnsureComponent<OperatedComponent>(patient);
        comp.GraphId = ChestGraph;
        comp.CurrentNode = node;
        comp.ActiveEdgeId = null;
        comp.CompletedSteps = 0;
        return comp;
    }

    private void RaiseStep(EntityUid patient, EntityUid surgeon, string edgeId, int stepIndex, EntityUid tool)
    {
        var comp = SComp<OperatedComponent>(patient);
        comp.ActiveEdgeId = edgeId;

        var ev = new SurgeryStepDoAfterEvent(edgeId, stepIndex);
        var args = new DoAfterArgs(SEntMan, surgeon, TimeSpan.FromSeconds(1), ev, patient, patient, tool);
        ev.DoAfter = new SharedDoAfter(0, args, TimeSpan.Zero);
        SEntMan.EventBus.RaiseLocalEvent(patient, (object) ev);
    }
}
