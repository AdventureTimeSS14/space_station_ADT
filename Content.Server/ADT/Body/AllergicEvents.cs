using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Body;

/// <summary>
/// When allergy triggered by allergens in bloodstream.
/// </summary>
[ByRefEvent]
public struct AllergyTriggeredEvent;

/// <summary>
/// When allergy stack has faded to zero.
/// </summary>
[ByRefEvent]
public struct AllergyFadedEvent;