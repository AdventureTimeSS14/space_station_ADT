using Content.Shared.ADT.Disease;

namespace Content.Server.ADT.Disease.Components;

public sealed class DiseaseInfectionSpreadEvent : EntityEventArgs
{
    public EntityUid Owner { get; init; } = default!;
    public DiseasePrototype Disease { get; init; } = default!;
    public float Range { get; init; } = default!;
}
