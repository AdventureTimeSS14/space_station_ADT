using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using SharedDoAfter = Content.Shared.DoAfter.DoAfter;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.ADT.Surgery;

[TestFixture]
public sealed class SurgeryLimbTest : GameTest
{
    private const string AmputateGraph = "SurgeryAmputateArmLeft";
    private const string ReattachGraph = "SurgeryReattachArmLeft";
    private const string Arm = "ArmLeft";
    private const string Hand = "HandLeft";
    private const int MaxAttempts = 200;

    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private DetachableOrganSystem _detachable = default!;
    [SidedDependency(Side.Server)] private StatusEffectsSystem _status = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void AmputateAndReattachArm()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");

        var originalArm = GetOrgan(patient, Arm);
        var originalHand = GetOrgan(patient, Hand);
        Assert.That(originalArm, Is.Not.Null, "patient must start with a left arm");
        Assert.That(originalHand, Is.Not.Null, "patient must start with a left hand");

        SelectGraph(patient, surgeon, AmputateGraph);
        var comp = SComp<OperatedComponent>(patient);
        Assert.That(comp.CurrentNode, Is.EqualTo("Attached"));

        RunEdge(patient, surgeon, "Cut", SSpawn("Scalpel"));
        Assert.That(comp.CurrentNode, Is.EqualTo("Cut"));
        RunEdge(patient, surgeon, "ClampBleeders", SSpawn("Hemostat"));
        RunEdge(patient, surgeon, "RetractSkin", SSpawn("Retractor"));
        Assert.That(GetOrgan(patient, Arm), Is.Not.Null, "arm must stay attached until the saw step finishes");
        RunEdge(patient, surgeon, "Saw", SSpawn("Saw"));
        Assert.That(comp.CurrentNode, Is.EqualTo("Amputated"));

        Assert.That(GetOrgan(patient, Arm), Is.Null, "arm must be detached after amputation");
        Assert.That(GetOrgan(patient, Hand), Is.Null, "hand must leave with the arm");

        var detachedArm = FindDetachedBody(Arm, patient, surgeon);
        Assert.That(detachedArm, Is.Not.Null, "amputation must leave a detached limb behind");
        Assert.That(_detachable.TryGetDetachedOrgan(detachedArm!.Value, out var detachedOrgan), Is.True);
        Assert.That(detachedOrgan, Is.EqualTo(originalArm));

        RunEdge(patient, surgeon, "Cauterize", SSpawn("Cautery"));
        Assert.That(comp.CurrentNode, Is.EqualTo("Cauterized"));

        SelectGraph(patient, surgeon, ReattachGraph);
        Assert.That(comp.GraphId?.Id, Is.EqualTo(ReattachGraph));
        Assert.That(comp.CurrentNode, Is.EqualTo("Amputated"));

        RunEdge(patient, surgeon, "OpenStump", SSpawn("Scalpel"));
        Assert.That(comp.CurrentNode, Is.EqualTo("StumpOpened"));
        RunEdge(patient, surgeon, "Attach", detachedArm.Value);
        Assert.That(comp.CurrentNode, Is.EqualTo("LimbPlaced"));
        RunEdge(patient, surgeon, "ApplyBoneGelFirst", SSpawn("ADTBoneGel"));
        RunEdge(patient, surgeon, "SetBone", SSpawn("ADTBoneSetter"));
        RunEdge(patient, surgeon, "ApplyBoneGelSecond", SSpawn("ADTBoneGel"));
        Assert.That(comp.CurrentNode, Is.EqualTo("Attached"));

        var restoredArm = GetOrgan(patient, Arm);
        Assert.That(restoredArm, Is.EqualTo(originalArm), "the same arm must be reattached");
        Assert.That(SComp<OrganComponent>(restoredArm!.Value).Body, Is.EqualTo(patient));
        Assert.That(GetOrgan(patient, Hand), Is.EqualTo(originalHand), "hand must come back with the arm");
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CannotSwitchGraphMidOperation()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");

        SelectGraph(patient, surgeon, AmputateGraph);
        RunEdge(patient, surgeon, "Cut", SSpawn("Scalpel"));

        var comp = SComp<OperatedComponent>(patient);
        Assert.That(comp.CurrentNode, Is.EqualTo("Cut"));

        SelectGraph(patient, surgeon, ReattachGraph);

        Assert.That(comp.GraphId?.Id, Is.EqualTo(AmputateGraph));
        Assert.That(comp.CurrentNode, Is.EqualTo("Cut"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ReattachStumpRequiresMissingLimb()
    {
        var patient = SpawnPatient();
        var surgeon = SSpawn("MobHuman");

        SelectGraph(patient, surgeon, ReattachGraph);
        var comp = SComp<OperatedComponent>(patient);
        var graph = SProtoMan.Index<SurgeryGraphPrototype>(ReattachGraph);
        var openStump = graph.Nodes.Single(n => n.Name == comp.CurrentNode).Edges.Single(e => e.Id == "OpenStump");

        Assert.That(openStump.Conditions.All(c => c.CheckWithInvert(patient, SEntMan)), Is.False,
            "stump cannot be opened while the arm is still attached");

        _body.TryRemoveOrgan(GetOrgan(patient, Arm)!.Value);

        Assert.That(openStump.Conditions.All(c => c.CheckWithInvert(patient, SEntMan)), Is.True);
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
        if (!_body.TryGetOrgansWithComponent<OrganComponent>(patient, out var organs))
            return null;

        foreach (var organ in organs)
        {
            if (organ.Comp.Category == category)
                return organ.Owner;
        }

        return null;
    }

    private EntityUid? FindDetachedBody(string category, params EntityUid[] exclude)
    {
        foreach (var (uid, _) in SEntMan.AllEntities<BodyComponent>().Select(e => (e.Owner, e.Comp)).ToList())
        {
            if (exclude.Contains(uid))
                continue;

            if (_detachable.TryGetDetachedOrgan(uid, out var organ)
                && SComp<OrganComponent>(organ).Category == category)
                return uid;
        }

        return null;
    }

    private void SelectGraph(EntityUid patient, EntityUid surgeon, string graphId)
    {
        var message = new SurgerySelectGraphMessage(graphId) { Actor = surgeon };
        SEntMan.EventBus.RaiseLocalEvent(patient, message);
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

                var ev = new SurgeryStepDoAfterEvent(edgeId, stepIndex);
                var args = new DoAfterArgs(SEntMan, surgeon, TimeSpan.FromSeconds(1), ev, patient, patient, tool);
                ev.DoAfter = new SharedDoAfter(0, args, TimeSpan.Zero);
                SEntMan.EventBus.RaiseLocalEvent(patient, (object) ev);
            }
        }

        Assert.That(comp.IsOperating, Is.False);
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
