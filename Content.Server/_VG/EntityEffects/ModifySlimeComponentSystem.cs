using Content.Shared._VG.EntityEffects;
using Content.Shared._VG.Xenobiology.Components;
using Content.Shared.EntityEffects;

namespace Content.Server._VG.EntityEffects;

public sealed partial class ModifySlimeComponentSystem : EntityEffectSystem<SlimeComponent, ModifySlimeComponent>
{
    protected override void Effect(Entity<SlimeComponent> entity, ref EntityEffectEvent<ModifySlimeComponent> args)
    {
        var uid = entity.Owner;
        var slime = entity.Comp;
        var effect = args.Effect;

        slime.ExtractsProduced += effect.ExtractBonus ?? 0;
        slime.MaxOffspring += effect.OffspringBonus ?? 0;

        if (effect.ChanceModifier is { } chanceMod)
            slime.MutationChance = Math.Clamp(slime.MutationChance + chanceMod, 0f, 1f);
    }
}