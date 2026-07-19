namespace Content.Server.ADT.Body;

/// <summary>
/// Grants immunity to asphyxiation (suffocation) damage handled by the RespiratorSystem.
/// While present the entity never suffocates, regardless of the gas it is breathing.
/// </summary>
[RegisterComponent]
public sealed partial class BreathingImmunityComponent : Component
{
}
