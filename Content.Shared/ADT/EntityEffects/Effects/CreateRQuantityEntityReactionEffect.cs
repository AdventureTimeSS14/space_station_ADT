using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.EntityEffects.Effects;

[ImplicitDataDefinitionForInheritors]
public sealed partial class CreateRQuantityEntityReactionEffect : EntityEffect
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Entity = default!;

    [DataField]
    public int MaxEntities = 1;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        var ev = new CreateRQuantityEntityEvent(Entity, MaxEntities, target);
        raiser.RaiseEffectEvent(target, ev, scale, user);
    }

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-create-random-entity-reaction",
            ("amount", MaxEntities),
            ("entname", GuidebookTextHelpers.LocalizedEntityName(prototype, Entity)));
    }
}

public sealed partial class CreateRQuantityEntityEvent : EntityEffectBase<CreateRQuantityEntityEvent>
{
    public EntityUid Target;
    public string Entity;
    public int MaxEntities;

    public CreateRQuantityEntityEvent(string entity, int maxEntities, EntityUid target)
    {
        Entity = entity;
        MaxEntities = maxEntities;
        Target = target;
    }
}