using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Body.Prosthesis;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Flash.Components;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Wires;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using SharedDoAfter = Content.Shared.DoAfter.DoAfter;

namespace Content.IntegrationTests.Tests.ADT.Surgery;

[TestFixture]
public sealed class SurgeryCyberTest : GameTest
{
    private const int MaxAttempts = 200;

    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private RoboticPartSystem _robotic = default!;
    [SidedDependency(Side.Server)] private StandingStateSystem _standing = default!;
    [SidedDependency(Side.Server)] private SharedWiresSystem _wires = default!;
    [SidedDependency(Side.Server)] private StatusEffectsSystem _status = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void IpcUsesOnlyCyberGraphs()
    {
        var ipc = SSpawn("MobIPC");
        var surgeon = SSpawn("MobHuman");

        SelectGraph(ipc, surgeon, "SurgeryAmputateArmLeft");
        Assert.That(SEntMan.TryGetComponent<OperatedComponent>(ipc, out var organic) && organic.GraphId != null, Is.False);

        SelectGraph(ipc, surgeon, "SurgeryChestOrgans");
        Assert.That(SEntMan.TryGetComponent<OperatedComponent>(ipc, out organic) && organic.GraphId != null, Is.False);

        SelectGraph(ipc, surgeon, "SurgeryCyberDetachArmLeft");
        Assert.That(SComp<OperatedComponent>(ipc).GraphId?.Id, Is.EqualTo("SurgeryCyberDetachArmLeft"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void IpcLimbIsDetachedWithoutPainOrAnesthesia()
    {
        var ipc = SSpawn("MobIPC");
        var surgeon = SSpawn("MobHuman");
        var arm = GetOrgan(ipc, "ArmLeft");
        Assert.That(arm, Is.Not.Null);

        SelectGraph(ipc, surgeon, "SurgeryCyberDetachArmLeft");
        var trained = SEntMan.EnsureComponent<SurgerySkillComponent>(surgeon);
        Assert.That(trained, Is.Not.Null);

        RunEdge(ipc, surgeon, "Unscrew", SSpawn("Screwdriver"));
        RunEdge(ipc, surgeon, "OpenHatch", SSpawn("Crowbar"));
        Assert.That(GetOrgan(ipc, "ArmLeft"), Is.EqualTo(arm));
        RunEdge(ipc, surgeon, "Disconnect", SSpawn("Wrench"));

        Assert.That(GetOrgan(ipc, "ArmLeft"), Is.Null);
        Assert.That(GetOrgan(ipc, "HandLeft"), Is.Null, "hand must leave with the arm");
        Assert.That(SComp<DamageableComponent>(ipc).Damage.DamageDict.Values.All(value => value == 0), Is.True, "trained mechanical surgery must not hurt the patient");
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void IpcRejectsOrganicLimb()
    {
        var ipc = SSpawn("MobIPC");
        var surgeon = SSpawn("MobHuman");
        var humanArm = SSpawn("OrganHumanArmLeft");
        var ipcArm = GetOrgan(ipc, "ArmLeft")!.Value;

        _body.TryRemoveOrgan(ipcArm);
        SelectGraph(ipc, surgeon, "SurgeryCyberAttachArmLeft");
        RunEdge(ipc, surgeon, "OpenSocket", SSpawn("Screwdriver"));

        Step(ipc, surgeon, "Attach", 0, humanArm);

        Assert.That(GetOrgan(ipc, "ArmLeft"), Is.Null, "an organic arm must not fit the mechanical socket");
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ProsthesisReplacesArmWithHandAndNoRejection()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");
        var kit = SSpawn("ADTProsthesisRegularArmLeft");

        SelectGraph(patient, surgeon, "SurgeryAmputateArmLeft");
        RunEdge(patient, surgeon, "Cut", SSpawn("Scalpel"));
        RunEdge(patient, surgeon, "ClampBleeders", SSpawn("Hemostat"));
        RunEdge(patient, surgeon, "RetractSkin", SSpawn("Retractor"));
        var before = SComp<DamageableComponent>(patient).Damage.DamageDict.GetValueOrDefault("Poison");

        RunEdge(patient, surgeon, "ReplaceWithProsthesis", kit);

        var arm = GetOrgan(patient, "ArmLeft");
        var hand = GetOrgan(patient, "HandLeft");
        Assert.That(arm, Is.Not.Null);
        Assert.That(hand, Is.Not.Null, "prosthetic arm must come with its hand");
        Assert.That(SEntMan.HasComponent<ProsthesisComponent>(arm!.Value), Is.True);
        Assert.That(_robotic.GetKind(patient, "ArmLeft"), Is.EqualTo(OrganKind.Robotic));
        Assert.That(SComp<DamageableComponent>(patient).Damage.DamageDict.GetValueOrDefault("Poison"), Is.EqualTo(before), "prosthesis must not cause rejection");

        Step(patient, surgeon, "Cauterize", 0, SSpawn("Cautery"));
        SComp<OperatedComponent>(patient).GraphId = null;

        SelectGraph(patient, surgeon, "SurgeryAmputateArmLeft");
        Assert.That(SComp<OperatedComponent>(patient).GraphId, Is.Null, "a prosthetic arm is not amputated with a saw");

        SelectGraph(patient, surgeon, "SurgeryCyberDetachArmLeft");
        Assert.That(SComp<OperatedComponent>(patient).GraphId?.Id, Is.EqualTo("SurgeryCyberDetachArmLeft"));
    }

    [TestCase("MobSlimePerson")]
    [TestCase("MobDiona")]
    [TestCase("MobResomi")]
    [RunOnSide(Side.Server)]
    public void ForbiddenSpeciesRejectProsthesis(string mob)
    {
        var patient = SSpawn(mob);
        SEntMan.EnsureComponent<OperatedComponent>(patient);
        _status.TryAddStatusEffectDuration(patient, "StatusEffectAnesthetized", TimeSpan.FromMinutes(30));
        var surgeon = SSpawn("MobHuman");
        SEntMan.EnsureComponent<SurgerySkillComponent>(surgeon);
        var original = GetOrgan(patient, "ArmLeft");

        SelectGraph(patient, surgeon, "SurgeryAmputateArmLeft");
        RunEdge(patient, surgeon, "Cut", SSpawn("Scalpel"));
        RunEdge(patient, surgeon, "ClampBleeders", SSpawn("Hemostat"));
        RunEdge(patient, surgeon, "RetractSkin", SSpawn("Retractor"));

        Step(patient, surgeon, "ReplaceWithProsthesis", 0, SSpawn("ADTProsthesisRegularArmLeft"));

        Assert.That(GetOrgan(patient, "ArmLeft"), Is.EqualTo(original));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CheapLegSlowsPatient()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");
        var speed = SComp<MovementSpeedModifierComponent>(patient).CurrentSprintSpeed;

        SelectGraph(patient, surgeon, "SurgeryAmputateLegLeft");
        RunEdge(patient, surgeon, "Cut", SSpawn("Scalpel"));
        RunEdge(patient, surgeon, "ClampBleeders", SSpawn("Hemostat"));
        RunEdge(patient, surgeon, "RetractSkin", SSpawn("Retractor"));
        RunEdge(patient, surgeon, "ReplaceWithProsthesis", SSpawn("ADTProsthesisCheapLegLeft"));

        Assert.That(GetOrgan(patient, "FootLeft"), Is.Not.Null);
        Assert.That(SComp<MovementSpeedModifierComponent>(patient).CurrentSprintSpeed, Is.LessThan(speed));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void PanelStaysClosedDuringOperation()
    {
        var ipc = SSpawn("MobIPC");
        var surgeon = SSpawn("MobHuman");

        Assert.That(_wires.CanTogglePanel((ipc, SComp<WiresPanelComponent>(ipc)), surgeon), Is.True);

        _standing.Down(ipc, force: true);
        SelectGraph(ipc, surgeon, "SurgeryCyberDetachArmLeft");

        Assert.That(_wires.CanTogglePanel((ipc, SComp<WiresPanelComponent>(ipc)), surgeon), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void IpcEyesAreEyesAndImmuneToFlash()
    {
        var ipc = SSpawn("MobIPC");

        Assert.That(GetOrgan(ipc, "Eyes"), Is.Not.Null);
        Assert.That(SEntMan.HasComponent<FlashImmunityComponent>(ipc), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ProsthesisMarkingNamesMatchExistingMarkings()
    {
        var names = SProtoMan.EnumeratePrototypes<MarkingPrototype>()
            .Where(marking => RoboticPartSystem.IsProsthesisMarking(marking.ID))
            .Select(marking => marking.ID)
            .ToList();

        Assert.That(names, Does.Contain("CyberlimbRArmBishop"));
        Assert.That(names, Does.Contain("AugmentsRoboticArmLeft"));
        Assert.That(names, Does.Contain("ADTProstheticArmLeftMoth"));
        Assert.That(names, Does.Contain("ReptilianImplantCybersunArmL"));
    }

    private EntityUid SpawnPatient()
    {
        var patient = SSpawn("MobHuman");
        SEntMan.EnsureComponent<OperatedComponent>(patient);
        _status.TryAddStatusEffectDuration(patient, "StatusEffectAnesthetized", TimeSpan.FromMinutes(30));
        return patient;
    }

    private EntityUid? GetOrgan(EntityUid patient, string category)
    {
        foreach (var organ in _body.EnumerateOrgans<OrganComponent>(patient))
        {
            if (organ.Comp1.Category == category)
                return organ.Owner;
        }

        return null;
    }

    private void SelectGraph(EntityUid patient, EntityUid surgeon, string graphId)
    {
        var message = new SurgerySelectGraphMessage(graphId) { Actor = surgeon };
        SEntMan.EventBus.RaiseLocalEvent(patient, message);
    }

    private void Step(EntityUid patient, EntityUid surgeon, string edgeId, int stepIndex, EntityUid tool)
    {
        SComp<OperatedComponent>(patient).ActiveEdgeId = edgeId;
        var ev = new SurgeryStepDoAfterEvent(edgeId, stepIndex);
        var args = new DoAfterArgs(SEntMan, surgeon, TimeSpan.FromSeconds(1), ev, patient, patient, tool);
        ev.DoAfter = new SharedDoAfter(0, args, TimeSpan.Zero);
        SEntMan.EventBus.RaiseLocalEvent(patient, (object) ev);
    }

    private void RunEdge(EntityUid patient, EntityUid surgeon, string edgeId, EntityUid tool)
    {
        var comp = SComp<OperatedComponent>(patient);
        var graph = SProtoMan.Index(comp.GraphId!.Value);
        var node = graph.Nodes.Single(n => n.Name == comp.CurrentNode);
        var edge = AllEdges(node).Single(e => e.Id == edgeId);
        var startNode = comp.CurrentNode;

        for (var stepIndex = 0; stepIndex < edge.Steps.Count; stepIndex++)
        {
            var attempts = 0;
            while (comp.CurrentNode == startNode && comp.CompletedSteps <= stepIndex)
            {
                Assert.That(++attempts, Is.LessThanOrEqualTo(MaxAttempts),
                    $"step {stepIndex} of edge '{edgeId}' never succeeded");

                Step(patient, surgeon, edgeId, stepIndex, tool);
            }
        }
    }

    private IEnumerable<SurgeryGraphEdge> AllEdges(SurgeryGraphNode node)
    {
        foreach (var edge in node.Edges)
        {
            yield return edge;
        }

        foreach (var packageId in node.Packages)
        {
            foreach (var edge in SProtoMan.Index(packageId).Edges)
            {
                yield return edge;
            }
        }
    }
}
