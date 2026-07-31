using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.EntityEffects.Effects;

[ImplicitDataDefinitionForInheritors]
public sealed partial class CreateEntityReactionEffect : EntityEffect
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Entity = default!;

    [DataField]
    public uint Number = 1;

    [DataField]
    public float Delay = 0f;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        var ev = new CreateEntityEvent(Entity, Number, Delay, target);
        raiser.RaiseEffectEvent(target, ev, scale, user);
    }
}

public sealed partial class CreateEntityEvent : EntityEffectBase<CreateEntityEvent>
{
    public EntityUid Target;
    public string Entity;
    public uint Number;
    public float Delay;

    public CreateEntityEvent(string entity, uint number, float delay, EntityUid target)
    {
        Entity = entity;
        Number = number;
        Delay = delay;
        Target = target;
    }
}