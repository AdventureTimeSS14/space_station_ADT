using Content.Shared.ADT.EntityEffects;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ModifySlimeComponentSystem : EntityEffectSystem<SlimeComponent, ModifySlimeComponent>
{   
    protected override void Effect(Entity<SlimeComponent> entity, ref EntityEffectEvent<ModifySlimeComponent> args)
    {
        var uid = entity.Owner;
        var slime = entity.Comp;
        var effect = args.Effect;

        if (effect.ExtractBonus is { } extractBonus)
        {
            if (effect.MaxExtractBonus is { } maxExtract)
                slime.ExtractsProduced = Math.Min(slime.ExtractsProduced + extractBonus, maxExtract);
            else
                slime.ExtractsProduced += extractBonus;
        }

        if (effect.OffspringBonus is { } offspringBonus)
        {
            if (effect.MaxOffspringBonus is { } maxOffspring)
                slime.MaxOffspring = Math.Min(slime.MaxOffspring + offspringBonus, maxOffspring);
            else
                slime.MaxOffspring += offspringBonus;
        }

        if (effect.ChanceModifier is { } chanceMod)
            slime.MutationChance = Math.Clamp(slime.MutationChance + chanceMod, 0f, 1f);
    }
}