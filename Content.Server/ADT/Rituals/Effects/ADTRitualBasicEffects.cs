using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Rituals;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Rituals.Effects;

public sealed partial class ADTRitualSpawnEffect : ADTRitualEffect
{
    [DataField(required: true)]
    public List<EntProtoId> Entities = new();

    [DataField]
    public bool Pick;

    [DataField]
    public int Amount = 1;

    [DataField]
    public float Prob = 1f;

    [DataField]
    public int? StackCount;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        if (Entities.Count == 0)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        var stacks = entMan.System<SharedStackSystem>();
        var coords = entMan.GetComponent<TransformComponent>(args.Object).Coordinates;

        for (var i = 0; i < Amount; i++)
        {
            var batch = Pick ? new List<EntProtoId> { random.Pick(Entities) } : Entities;

            foreach (var proto in batch)
            {
                if (!random.Prob(Prob))
                    continue;

                var spawned = entMan.SpawnEntity(proto, coords);

                if (StackCount is { } count && entMan.HasComponent<StackComponent>(spawned))
                    stacks.SetCount(spawned, count);
            }
        }
    }
}

public sealed partial class ADTRitualDamageEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.Invoker;

    [DataField]
    public DamageSpecifier? Damage;

    [DataField]
    public float BloodLoss;

    [DataField]
    public float Prob = 1f;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var damageable = entMan.System<DamageableSystem>();
        var blood = entMan.System<SharedBloodstreamSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (!random.Prob(Prob))
                continue;

            if (Damage != null)
                damageable.TryChangeDamage(target, Damage, origin: args.Invoker);

            if (BloodLoss > 0f && entMan.HasComponent<BloodstreamComponent>(target))
                blood.TryModifyBloodLevel(target, -BloodLoss);
        }
    }
}

public sealed partial class ADTRitualIgniteEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.Tribe;

    [DataField]
    public float FireStacks = 6f;

    [DataField]
    public TimeSpan Knockdown = TimeSpan.FromSeconds(10);

    [DataField]
    public float Prob = 1f;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var flammable = entMan.System<FlammableSystem>();
        var stun = entMan.System<SharedStunSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (!random.Prob(Prob))
                continue;

            flammable.AdjustFireStacks(target, FireStacks, ignite: true);

            if (Knockdown > TimeSpan.Zero)
                stun.TryKnockdown(target, Knockdown);
        }
    }
}

public sealed partial class ADTRitualComponentsEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.Invokers;

    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public List<string> RemoveComponents = new();

    [DataField]
    public float Prob = 1f;

    [DataField]
    public bool Overwrite;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var factory = IoCManager.Resolve<IComponentFactory>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (!random.Prob(Prob))
                continue;

            foreach (var name in RemoveComponents)
            {
                if (factory.TryGetRegistration(name, out var registration))
                    entMan.RemoveComponent(target, registration.Type);
            }

            if (Components.Count > 0)
                entMan.AddComponents(target, Components, Overwrite);
        }
    }
}

public sealed partial class ADTRitualDropItemsEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.Tribe;

    [DataField]
    public float Prob = 1f;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var hands = entMan.System<SharedHandsSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (!random.Prob(Prob))
                continue;

            foreach (var held in hands.EnumerateHeld(target).ToArray())
            {
                hands.TryDrop(target, held, checkActionBlocker: false);
            }
        }
    }
}

public sealed partial class ADTRitualMessageEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.Tribe;

    [DataField]
    public LocId? Message;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public PopupType PopupType = PopupType.Large;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var popup = entMan.System<SharedPopupSystem>();
        var audio = entMan.System<SharedAudioSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (Message != null)
                popup.PopupEntity(Loc.GetString(Message), target, target, PopupType);

            if (Sound != null)
                audio.PlayEntity(Sound, target, target);
        }
    }
}

public sealed partial class ADTRitualStatusEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.Tribe;

    [DataField(required: true)]
    public EntProtoId StatusEffect = default!;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    [DataField]
    public float Prob = 1f;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var status = entMan.System<StatusEffectsSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (!random.Prob(Prob))
                continue;

            status.TryAddStatusEffect(target, StatusEffect, out _, Duration);
        }
    }
}
