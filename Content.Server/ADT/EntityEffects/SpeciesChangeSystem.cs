using Content.Shared.ADT.EntityEffects;
using Content.Server.Polymorph.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Polymorph;
using Content.Server.Polymorph.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class SpeciesChangeSystem : EntityEffectSystem<HumanoidProfileComponent, SpeciesChange>
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> entity, ref EntityEffectEvent<SpeciesChange> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        if (!_prototype.TryIndex(effect.NewSpecies, out var species))
            return;

        var config = new PolymorphConfiguration
        {
            Entity = species.Prototype,
            TransferDamage = true,
            Forced = true,
            Inventory = PolymorphInventoryChange.Transfer,
            RevertOnCrit = false,
            RevertOnDeath = false,
            TransferName = true,
        };

        var result = _polymorph.PolymorphEntity(uid, config);
        if (result.HasValue)
        {
            // Удаляем компонент, используя правильный тип
            if (TryComp<PolymorphedEntityComponent>(result.Value, out _))
                RemCompDeferred<PolymorphedEntityComponent>(result.Value);
        }
    }
}