using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.DoAfter;
using Content.Shared.Item.ItemToggle;
using Content.Shared.StatusEffectNew;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using SharedDoAfter = Content.Shared.DoAfter.DoAfter;

namespace Content.IntegrationTests.Tests.ADT.Surgery;

[TestFixture]
public sealed class SurgeryToolsCoverageTest : GameTest
{
    private const string ChestGraph = "SurgeryChestOrgans";
    private const string SurgeryToolTag = "SurgeryTool";
    private const int ImprovisedAttempts = 80;
    private const int ForbiddenAttempts = 60;

    [SidedDependency(Side.Server)] private StatusEffectsSystem _status = default!;
    [SidedDependency(Side.Server)] private OrganHealthSystem _organHealth = default!;
    [SidedDependency(Side.Server)] private ItemToggleSystem _toggle = default!;
    [SidedDependency(Side.Server)] private TagSystem _tag = default!;

    private sealed record Probe(string Quality, string Node, string Edge);

    private static readonly Probe[] Probes =
    {
        new("Incision", "ChestClosed", "Cut"),
        new("Clamping", "ChestIncised", "ClampBleeders"),
        new("Cautery", "ChestIncised", "Cauterize"),
        new("Retraction", "ChestBleedersClamped", "RetractSkin"),
        new("Sawing", "ChestSkinRetracted", "SawRibcage"),
        new("BoneMending", "ChestBoneSawed", "ApplyBoneGelFirst"),
        new("BoneSetting", "ChestBoneGelled", "SetBone"),
        new("OrganManipulation", "ChestOpen", "RemoveHeart"),
    };

    private sealed record ToolSpec(
        string Id,
        int Tier,
        float Speed,
        string[] Proper,
        string[] Improvised,
        string[]? ToggledProper = null,
        string[]? ToggledImprovised = null);

    private static readonly ToolSpec[] Tools =
    {
        new("Scalpel", 0, 1.15f, ["Incision"], ["Sawing"]),
        new("ScalpelAdvanced", 2, 0.55f, ["Incision"], ["Sawing"]),
        new("ScalpelLaser", 2, 0.55f, ["Incision"], ["Sawing"]),
        new("ScalpelShiv", 0, 1.15f, ["Incision"], ["Sawing"]),
        new("ADTSupermatterScalpel", 0, 1.15f, [], ["Incision", "Sawing"]),
        new("Cautery", 0, 1.15f, ["Cautery"], []),
        new("Drill", 0, 1.15f, [], []),
        new("Retractor", 0, 1.15f, ["Retraction"], []),
        new("Hemostat", 0, 1.15f, ["Clamping", "OrganManipulation"], []),
        new("ADTBoneSetter", 0, 1.15f, ["BoneSetting"], []),
        new("Saw", 0, 1.15f, ["Sawing"], ["Incision"]),
        new("SawElectric", 2, 0.55f, ["Sawing"], ["Incision"]),
        new("SawImprov", 0, 1.15f, ["Sawing"], ["Incision"]),
        new("SawAdvanced", 3, 0.4f, ["Sawing"], ["Incision"]),
        new("ADTSawImprovised", 0, 1.15f, [], ["Sawing", "Incision"]),
        new("ADTBoneGel", 0, 1.15f, ["BoneMending"], []),
        new("ADTSurgicalTape", 0, 1.15f, ["BoneMending"], []),
        new("ADTBloodFilter", 0, 1.15f, [], []),
        new("ADTLaserScalpel", 2, 0.55f, ["Incision"], ["Sawing"], ["Sawing"], ["Incision"]),
        new("ADTMechanicalPinches", 2, 0.55f, ["Retraction"], [], ["Clamping", "OrganManipulation"], []),
        new("ADTSearingTool", 2, 0.55f, ["Cautery"], [], [], []),
    };

    [Test]
    [RunOnSide(Side.Server)]
    public void EverySurgeryToolIsCovered()
    {
        var covered = Tools.Select(t => t.Id).ToHashSet();
        var missing = new List<string>();

        foreach (var proto in SProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || proto.Components.ContainsKey("Organ") || !proto.Components.ContainsKey("SurgeryTool") || covered.Contains(proto.ID))
                continue;

            var uid = SSpawn(proto.ID);
            if (_tag.HasTag(uid, SurgeryToolTag))
                missing.Add(proto.ID);

            SEntMan.DeleteEntity(uid);
        }

        Assert.That(missing, Is.Empty, "surgery tools missing from SurgeryToolsCoverageTest.Tools: " + string.Join(", ", missing));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ToolStatsMatchSpec()
    {
        var errors = new List<string>();

        foreach (var spec in Tools)
        {
            var tool = SSpawn(spec.Id);
            var comp = SComp<SurgeryToolComponent>(tool);

            if (comp.Tier != spec.Tier)
                errors.Add($"{spec.Id}: tier {comp.Tier}, expected {spec.Tier}");

            if (Math.Abs(comp.Speed - spec.Speed) > 0.001f)
                errors.Add($"{spec.Id}: speed {comp.Speed}, expected {spec.Speed}");

            SEntMan.DeleteEntity(tool);
        }

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ProbesAreValidGraphSteps()
    {
        var graph = SProtoMan.Index<Content.Shared.ADT.Surgery.Prototypes.SurgeryGraphPrototype>(ChestGraph);
        var errors = new List<string>();

        foreach (var probe in Probes)
        {
            var node = graph.Nodes.FirstOrDefault(n => n.Name == probe.Node);
            var edge = node?.Edges.FirstOrDefault(e => e.Id == probe.Edge);

            if (edge == null)
                errors.Add($"{probe.Quality}: edge {probe.Node}/{probe.Edge} not found");
            else if (edge.Steps[0].Quality?.Id != probe.Quality)
                errors.Add($"{probe.Quality}: edge {probe.Node}/{probe.Edge} first step uses {edge.Steps[0].Quality}");
        }

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ProperToolsAlwaysSucceedForTrainedSurgeon()
    {
        var errors = new List<string>();

        foreach (var spec in Tools)
        {
            foreach (var quality in spec.Proper)
            {
                for (var i = 0; i < 5; i++)
                {
                    if (!Advances(spec.Id, quality, trained: true, toggled: false, attempts: 1))
                        errors.Add($"{spec.Id}: proper quality {quality} failed on a trained single attempt (iteration {i})");
                }
            }

            foreach (var quality in spec.ToggledProper ?? [])
            {
                for (var i = 0; i < 5; i++)
                {
                    if (!Advances(spec.Id, quality, trained: true, toggled: true, attempts: 1))
                        errors.Add($"{spec.Id}: toggled quality {quality} failed on a trained single attempt (iteration {i})");
                }
            }
        }

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ImprovisedUseEventuallySucceeds()
    {
        var errors = new List<string>();

        foreach (var spec in Tools)
        {
            foreach (var quality in spec.Improvised)
            {
                if (!Advances(spec.Id, quality, trained: true, toggled: false, attempts: ImprovisedAttempts))
                    errors.Add($"{spec.Id}: improvised {quality} never succeeded in {ImprovisedAttempts} attempts");
            }

            foreach (var quality in spec.ToggledImprovised ?? [])
            {
                if (!Advances(spec.Id, quality, trained: true, toggled: true, attempts: ImprovisedAttempts))
                    errors.Add($"{spec.Id}: toggled improvised {quality} never succeeded in {ImprovisedAttempts} attempts");
            }
        }

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ToolsNeverSucceedOutsideTheirQualities()
    {
        var errors = new List<string>();

        foreach (var spec in Tools)
        {
            var offAllowed = spec.Proper.Concat(spec.Improvised).ToHashSet();
            var onAllowed = (spec.ToggledProper ?? []).Concat(spec.ToggledImprovised ?? []).ToHashSet();
            var toggleable = spec.ToggledProper != null;

            foreach (var probe in Probes)
            {
                if (!offAllowed.Contains(probe.Quality)
                    && Advances(spec.Id, probe.Quality, trained: true, toggled: false, attempts: ForbiddenAttempts))
                {
                    errors.Add($"{spec.Id}: completed {probe.Quality} but must not");
                }

                if (toggleable
                    && !onAllowed.Contains(probe.Quality)
                    && Advances(spec.Id, probe.Quality, trained: true, toggled: true, attempts: ForbiddenAttempts))
                {
                    errors.Add($"{spec.Id} (toggled): completed {probe.Quality} but must not");
                }
            }
        }

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void UntrainedSurgeonSometimesFailsWithProperTool()
    {
        foreach (var spec in Tools.Where(t => t.Proper.Length > 0))
        {
            var quality = spec.Proper[0];
            var failed = false;

            for (var i = 0; i < 80 && !failed; i++)
            {
                if (!Advances(spec.Id, quality, trained: false, toggled: false, attempts: 1))
                    failed = true;
            }

            Assert.That(failed, Is.True, $"{spec.Id}: untrained surgeon never failed {quality} in 80 attempts");
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ToggleActuallySwitchesToolMode()
    {
        foreach (var spec in Tools.Where(t => t.ToggledProper != null))
        {
            var tool = SSpawn(spec.Id);
            Assert.That(_toggle.IsActivated(tool), Is.False, $"{spec.Id} must start in the default mode");
            Assert.That(_toggle.TryActivate(tool), Is.True, $"{spec.Id} could not be toggled on");
            Assert.That(_toggle.IsActivated(tool), Is.True);
            Assert.That(_toggle.TryDeactivate(tool), Is.True, $"{spec.Id} could not be toggled off");
            SEntMan.DeleteEntity(tool);
        }
    }

    private bool Advances(string toolId, string quality, bool trained, bool toggled, int attempts)
    {
        var probe = Probes.First(p => p.Quality == quality);
        var patient = SSpawn("MobHuman");
        SEntMan.EnsureComponent<OperatedComponent>(patient);
        _status.TryAddStatusEffectDuration(patient, "StatusEffectAnesthetized", TimeSpan.FromMinutes(30));

        var surgeon = SSpawn("MobHuman");
        if (trained)
            SEntMan.EnsureComponent<SurgerySkillComponent>(surgeon);

        var tool = SSpawn(toolId);
        if (toggled)
            Assert.That(_toggle.TryActivate(tool), Is.True, $"{toolId} could not be toggled on");

        var comp = SComp<OperatedComponent>(patient);
        var advanced = false;

        for (var attempt = 0; attempt < attempts && !advanced; attempt++)
        {
            comp.GraphId = ChestGraph;
            comp.CurrentNode = probe.Node;
            comp.ActiveEdgeId = null;
            comp.CompletedSteps = 0;
            comp.IsOperating = false;
            comp.Surgeon = surgeon;

            var ev = new SurgeryStepDoAfterEvent(probe.Edge, 0);
            var args = new DoAfterArgs(SEntMan, surgeon, TimeSpan.FromSeconds(1), ev, patient, patient, tool);
            ev.DoAfter = new SharedDoAfter(0, args, TimeSpan.Zero);
            SEntMan.EventBus.RaiseLocalEvent(patient, (object) ev);

            advanced = quality == "OrganManipulation"
                ? !_organHealth.TryGetOrgan(patient, "Heart", out _)
                : comp.CurrentNode != probe.Node;
        }

        SEntMan.DeleteEntity(tool);
        SEntMan.DeleteEntity(surgeon);
        SEntMan.DeleteEntity(patient);
        return advanced;
    }
}
