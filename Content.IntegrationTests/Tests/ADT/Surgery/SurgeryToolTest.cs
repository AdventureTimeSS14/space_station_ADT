using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.DoAfter;
using Content.Shared.Item.ItemToggle;
using SharedDoAfter = Content.Shared.DoAfter.DoAfter;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.ADT.Surgery;

[TestFixture]
public sealed class SurgeryToolTest : GameTest
{
    private const string ChestGraph = "SurgeryChestOrgans";
    private const int MaxAttempts = 200;

    [SidedDependency(Side.Server)] private StatusEffectsSystem _status = default!;
    [SidedDependency(Side.Server)] private OrganHealthSystem _organHealth = default!;
    [SidedDependency(Side.Server)] private ItemToggleSystem _toggle = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void ToggledToolSwitchesQuality()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");
        SEntMan.EnsureComponent<SurgerySkillComponent>(surgeon);
        var comp = StartGraph(patient, ChestGraph, "ChestIncised");
        var pinches = SSpawn("ADTMechanicalPinches");

        RaiseStep(patient, surgeon, "ClampBleeders", 0, pinches);
        Assert.That(comp.CurrentNode, Is.EqualTo("ChestIncised"), "pinches must not clamp in retracting mode");

        Assert.That(_toggle.TryActivate(pinches), Is.True);
        RaiseStep(patient, surgeon, "ClampBleeders", 0, pinches);
        Assert.That(comp.CurrentNode, Is.EqualTo("ChestBleedersClamped"));

        RaiseStep(patient, surgeon, "RetractSkin", 0, pinches);
        Assert.That(comp.CurrentNode, Is.EqualTo("ChestBleedersClamped"), "pinches must not retract in clamping mode");

        Assert.That(_toggle.TryDeactivate(pinches), Is.True);
        RaiseStep(patient, surgeon, "RetractSkin", 0, pinches);
        Assert.That(comp.CurrentNode, Is.EqualTo("ChestSkinRetracted"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ImprovisedToolCompletesStep()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");
        var comp = StartGraph(patient, ChestGraph, "ChestClosed");
        var knife = SSpawn("KitchenKnife");

        for (var attempt = 0; comp.CurrentNode == "ChestClosed"; attempt++)
        {
            Assert.That(attempt, Is.LessThan(MaxAttempts), "knife never completed the incision");
            RaiseStep(patient, surgeon, "Cut", 0, knife);
        }

        Assert.That(comp.CurrentNode, Is.EqualTo("ChestIncised"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void WrongToolDoesNotAdvance()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");
        var comp = StartGraph(patient, ChestGraph, "ChestClosed");
        var crowbar = SSpawn("Crowbar");

        for (var attempt = 0; attempt < 20; attempt++)
        {
            RaiseStep(patient, surgeon, "Cut", 0, crowbar);
        }

        Assert.That(comp.CurrentNode, Is.EqualTo("ChestClosed"));
        Assert.That(comp.CompletedSteps, Is.Zero);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void FailedStepKeepsProgressAndCanBeRetried()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");
        var comp = StartGraph(patient, ChestGraph, "ChestClosed");
        var knife = SSpawn("KitchenKnife");

        var failures = 0;
        for (var attempt = 0; failures == 0 || comp.CurrentNode == "ChestClosed"; attempt++)
        {
            Assert.That(attempt, Is.LessThan(MaxAttempts), "step never succeeded after a failure");

            RaiseStep(patient, surgeon, "Cut", 0, knife);

            if (comp.CurrentNode != "ChestClosed")
            {
                if (failures == 0)
                    StartGraph(patient, ChestGraph, "ChestClosed");

                continue;
            }

            failures++;
            Assert.That(comp.CompletedSteps, Is.Zero);
            Assert.That(comp.IsOperating, Is.False, "failed step must not block a retry");
        }

        Assert.That(comp.CurrentNode, Is.EqualTo("ChestIncised"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void TrainedSurgeonNeverFailsWithProperTools()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");
        SEntMan.EnsureComponent<SurgerySkillComponent>(surgeon);
        var comp = StartGraph(patient, ChestGraph, "ChestClosed");
        var scalpel = SSpawn("Scalpel");
        var cautery = SSpawn("Cautery");

        for (var i = 0; i < 10; i++)
        {
            comp = StartGraph(patient, ChestGraph, "ChestClosed");
            RaiseStep(patient, surgeon, "Cut", 0, scalpel);
            Assert.That(comp.CurrentNode, Is.EqualTo("ChestIncised"), $"incision failed on iteration {i}");

            RaiseStep(patient, surgeon, "Cauterize", 0, cautery);
            Assert.That(comp.CurrentNode, Is.EqualTo("ChestClosed"), $"cauterization failed on iteration {i}");
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void FailedOrganRemovalDamagesOrgan()
    {
        for (var patientIndex = 0; patientIndex < 50; patientIndex++)
        {
            var patient = SpawnPatient();
            var surgeon = SSpawn("MobHuman");
            var comp = StartGraph(patient, ChestGraph, "ChestOpen");
            var crowbar = SSpawn("Crowbar");

            Assert.That(_organHealth.TryGetOrgan(patient, "Heart", out var heart), Is.True);

            RaiseStep(patient, surgeon, "RemoveHeart", 0, crowbar);

            if (!_organHealth.TryGetOrgan(patient, "Heart", out _))
                continue;

            Assert.That(comp.CompletedSteps, Is.Zero);
            Assert.That(heart.Comp.Damage, Is.GreaterThan(0f), "failed organ removal must damage the organ");
            return;
        }

        Assert.Fail("organ removal never failed");
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void StaleOperatingFlagDoesNotLockPatient()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");
        SEntMan.EnsureComponent<SurgerySkillComponent>(surgeon);
        var comp = StartGraph(patient, ChestGraph, "ChestClosed");
        comp.IsOperating = true;
        comp.DoAfter = null;

        SEntMan.EventBus.RaiseLocalEvent(patient, new SurgerySelectGraphMessage("SurgeryAbdomenOrgans") { Actor = surgeon });

        Assert.That(comp.IsOperating, Is.False);
        Assert.That(comp.GraphId?.Id, Is.EqualTo("SurgeryAbdomenOrgans"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ReturningToStartNodeEndsOperation()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");
        SEntMan.EnsureComponent<SurgerySkillComponent>(surgeon);
        var comp = StartGraph(patient, ChestGraph, "ChestIncised");

        RaiseStep(patient, surgeon, "Cauterize", 0, SSpawn("Cautery"));

        Assert.That(comp.CurrentNode, Is.EqualTo("ChestClosed"));
        Assert.That(comp.GraphId, Is.Null, "a closed patient must not keep an active graph");
    }

    private EntityUid SpawnPatient()
    {
        var patient = SSpawn("MobHuman");
        SEntMan.EnsureComponent<OperatedComponent>(patient);
        _status.TryAddStatusEffectDuration(patient, "StatusEffectAnesthetized", TimeSpan.FromMinutes(30));
        return patient;
    }

    private OperatedComponent StartGraph(EntityUid patient, string graphId, string node)
    {
        var comp = SEntMan.EnsureComponent<OperatedComponent>(patient);
        comp.GraphId = graphId;
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
