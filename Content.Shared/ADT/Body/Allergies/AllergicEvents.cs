using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;

namespace Content.Shared.ADT.Body.Allergies;

[ByRefEvent]
public record struct GetReagentEffectsEvent(ReagentId Reagent, EntityEffect[] Effects);

public sealed class AllergyTriggeredEvent : EntityEventArgs
{
}

public sealed class AllergyFadedEvent : EntityEventArgs
{
}
