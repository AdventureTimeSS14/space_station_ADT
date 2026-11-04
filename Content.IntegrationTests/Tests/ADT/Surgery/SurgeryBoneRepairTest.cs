using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.ADT.Body.Bones;
using Content.Server.ADT.Body.Shrapnel;
using Content.Shared.ADT.Body.Bones;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using SharedDoAfter = Content.Shared.DoAfter.DoAfter;

namespace Content.IntegrationTests.Tests.ADT.Surgery;

[TestFixture]
public sealed class SurgeryBoneRepairTest : GameTest
{
    [SidedDependency(Side.Server)] private StatusEffectsSystem _status = default!;
    [SidedDependency(Side.Server)] private ADTFractureSystem _fracture = default!;
    [SidedDependency(Side.Server)] private ADTShrapnelSystem _shrapnel = default!;
    [SidedDependency(Side.Server)] private DamageableSystem _damageable = default!;
    [SidedDependency(Side.Server)] private IPrototypeManager _proto = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void BrokenArmIsMendedBySurgery()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();

        Break(patient, "ArmLeft", "ADTFractureClosed");
        Assert.That(_fracture.HasFracture(patient, "ArmLeft"), Is.True);
        Assert.That(SComp<ADTFracturedComponent>(patient).DoAfterModifier, Is.GreaterThan(1f));

        var comp = SEntMan.EnsureComponent<OperatedComponent>(patient);
        comp.GraphId = "SurgeryBoneRepairArmLeft";
        comp.CurrentNode = "SkinRetracted";

        RaiseStep(patient, surgeon, "GlueBone", 0, SSpawn("ADTBoneGel"));
        RaiseStep(patient, surgeon, "SetBone", 0, SSpawn("ADTBoneSetter"));
        RaiseStep(patient, surgeon, "FinishBone", 0, SSpawn("ADTBoneGel"));

        Assert.That(_fracture.HasFracture(patient, "ArmLeft"), Is.False);
        Assert.That(SEntMan.HasComponent<ADTFracturedComponent>(patient), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void SplintRemovesLegSlowdown()
    {
        var patient = SpawnPatient();

        Break(patient, "LegLeft", "ADTFractureClosed");
        Assert.That(SComp<ADTFracturedComponent>(patient).SpeedModifier, Is.LessThan(1f));

        Assert.That(_fracture.TryGetBone(patient, "LegLeft", out var bone), Is.True);
        Assert.That(_fracture.TrySplint(patient, bone!.Value, 100f), Is.True);
        Assert.That(SComp<ADTFracturedComponent>(patient).SpeedModifier, Is.EqualTo(1f));
        Assert.That(_fracture.HasFracture(patient, "LegLeft"), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void OpenFractureCannotBeSplinted()
    {
        var patient = SpawnPatient();

        Break(patient, "ArmRight", "ADTFractureOpen");
        Assert.That(_fracture.TryGetBone(patient, "ArmRight", out var bone), Is.True);
        Assert.That(_fracture.TrySplint(patient, bone!.Value, 100f), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void FractureDamageIsNotHealedUntilSplinted()
    {
        var patient = SpawnPatient();
        var blunt = new DamageSpecifier { DamageDict = new() { { "Blunt", 30 } } };
        _damageable.TryChangeDamage(patient, blunt, ignoreResistances: true);

        Break(patient, "ArmLeft", "ADTFractureClosed");
        _damageable.TryChangeDamage(patient, -blunt, ignoreResistances: true);
        Assert.That(_damageable.GetDamagePerGroup(patient)["Brute"].Float(), Is.GreaterThanOrEqualTo(9.9f));

        Assert.That(_fracture.TryGetBone(patient, "ArmLeft", out var bone), Is.True);
        _fracture.TrySplint(patient, bone!.Value, 100f);
        _damageable.TryChangeDamage(patient, -blunt, ignoreResistances: true);
        Assert.That(_damageable.GetDamagePerGroup(patient)["Brute"].Float(), Is.LessThan(0.1f));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RobotLimbDoesNotBreak()
    {
        var patient = SSpawn("MobIPC");
        Assert.That(_fracture.TryGetBone(patient, "ArmLeft", out var bone), Is.True);
        Assert.That(_fracture.ApplyFracture(patient, bone!.Value, _proto.Index<ADTFractureTypePrototype>("ADTFractureClosed"), true), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ShrapnelIsRemovedBySurgery()
    {
        var patient = SpawnPatient();
        var surgeon = SpawnSurgeon();

        Assert.That(_shrapnel.Embed(patient, "ArmLeft", "ADTShrapnel", true), Is.True);
        Assert.That(_shrapnel.HasEmbedded(patient, "ArmLeft"), Is.True);

        var comp = SEntMan.EnsureComponent<OperatedComponent>(patient);
        comp.GraphId = "SurgeryShrapnelRemovalArmLeft";
        comp.CurrentNode = "SkinRetracted";

        RaiseStep(patient, surgeon, "RemoveObjects", 0, SSpawn("Hemostat"));

        Assert.That(_shrapnel.HasEmbedded(patient, "ArmLeft"), Is.False);
    }

    private void Break(EntityUid patient, string category, string type)
    {
        Assert.That(_fracture.TryGetBone(patient, category, out var bone), Is.True);
        Assert.That(_fracture.ApplyFracture(patient, bone!.Value, _proto.Index<ADTFractureTypePrototype>(type), true), Is.True);

        var fractured = SComp<ADTFracturedComponent>(patient);
        fractured.IgnoreUntil = TimeSpan.Zero;
        _fracture.Refresh(patient);
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
