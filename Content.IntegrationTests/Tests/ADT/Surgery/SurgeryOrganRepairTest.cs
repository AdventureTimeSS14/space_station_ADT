using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.VoiceOfGod;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes.Conditions;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
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
        _organHealth.SetDamage((heart.Owner, heart.Comp), 60f);
        Assert.That(heart.Comp.Stage, Is.EqualTo(OrganStage.Damaged));

        RaiseStep(patient, surgeon, "RepairHeart", 0, SSpawn("Hemostat"));
        RaiseStep(patient, surgeon, "RepairHeart", 1, SSpawn("Cautery"));

        Assert.That(heart.Comp.Damage, Is.LessThanOrEqualTo(heart.Comp.MaxDamage * 0.1f));
        Assert.That(heart.Comp.Stage, Is.EqualTo(OrganStage.Healthy));
        Assert.That(comp.CurrentNode, Is.EqualTo("ChestOpen"));
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
        Assert.That(heart.Comp.MaxDamage, Is.EqualTo(75f));

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
