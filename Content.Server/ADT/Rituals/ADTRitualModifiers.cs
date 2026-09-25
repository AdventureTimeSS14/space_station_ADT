using System.Linq;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.Rituals;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Rituals;

public sealed partial class ADTRitualShamanModifier : ADTRitualModifier
{
    [DataField]
    public float? FailChance;

    [DataField]
    public float? DisasterChance;

    public override void Apply(IEntityManager entMan, ADTRitualArgs args, ref float fail, ref float disaster)
    {
        if (entMan.TryGetComponent<ADTAshWalkerComponent>(args.Invoker, out var walker) && walker.Shaman)
            return;

        if (FailChance is { } newFail)
            fail = newFail;

        if (DisasterChance is { } newDisaster)
            disaster = newDisaster;
    }
}

public sealed partial class ADTRitualHealthyInvokerModifier : ADTRitualModifier
{
    [DataField]
    public float Threshold = 90f;

    [DataField]
    public float? FailChance;

    [DataField]
    public float? DisasterChance;

    public override void Apply(IEntityManager entMan, ADTRitualArgs args, ref float fail, ref float disaster)
    {
        if (!entMan.TryGetComponent<DamageableComponent>(args.Invoker, out var damageable))
            return;

        if (damageable.TotalDamage >= Threshold)
            return;

        if (FailChance is { } newFail)
            fail = newFail;

        if (DisasterChance is { } newDisaster)
            disaster = newDisaster;
    }
}

public sealed partial class ADTRitualThingSpeciesModifier : ADTRitualModifier
{
    [DataField(required: true)]
    public List<ProtoId<SpeciesPrototype>> Species = new();

    [DataField]
    public float? FailChance;

    [DataField]
    public float? DisasterChance;

    public override void Apply(IEntityManager entMan, ADTRitualArgs args, ref float fail, ref float disaster)
    {
        foreach (var thing in args.UsedThings)
        {
            if (!entMan.HasComponent<MobStateComponent>(thing))
                continue;

            if (entMan.TryGetComponent<HumanoidProfileComponent>(thing, out var profile)
                && Species.Contains(profile.Species))
                return;
        }

        if (FailChance is { } newFail)
            fail = newFail;

        if (DisasterChance is { } newDisaster)
            disaster = newDisaster;
    }
}

public sealed partial class ADTRitualConsciousInvokersModifier : ADTRitualModifier
{
    [DataField]
    public float FailChance = 0.2f;

    [DataField]
    public float DisasterChance = 0.2f;

    public override void Apply(IEntityManager entMan, ADTRitualArgs args, ref float fail, ref float disaster)
    {
        var mobState = entMan.System<MobStateSystem>();
        var conscious = args.Invokers.Count(uid =>
            entMan.HasComponent<MobStateComponent>(uid) && !mobState.IsIncapacitated(uid));

        fail += FailChance * conscious;
        disaster += DisasterChance * conscious;
    }
}
