using Content.Shared.ADT.Atmos.Miasma;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;

namespace Content.Server.ADT.Atmos.Rotting;

public sealed class EmbalmEffectSystem : EntityEffectSystem<MetaDataComponent, Embalm>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Embalm> args)
    {
        EnsureComp<EmbalmedComponent>(entity);
    }
}
