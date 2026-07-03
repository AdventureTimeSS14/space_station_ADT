using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;

namespace Content.Shared.ADT.Body.Allergies;

public sealed class GetReagentEffectsEvent : EntityEventArgs
{
    public ReagentId Reagent { get; }
    public EntityEffect[] Effects { get; set; }

    public GetReagentEffectsEvent(ReagentId reagent, EntityEffect[] effects)
    {
        Reagent = reagent;
        Effects = effects;
    }
}

public sealed class AllergyTriggeredEvent : EntityEventArgs
{
}

public sealed class AllergyFadedEvent : EntityEventArgs
{
}
