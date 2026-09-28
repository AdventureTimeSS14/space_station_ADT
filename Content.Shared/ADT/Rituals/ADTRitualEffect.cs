namespace Content.Shared.ADT.Rituals;

public readonly record struct ADTRitualArgs(
    EntityUid Object,
    ADTRitualPrototype Ritual,
    EntityUid Invoker,
    IReadOnlyList<EntityUid> Invokers,
    IReadOnlyList<EntityUid> UsedThings);

[ImplicitDataDefinitionForInheritors]
public abstract partial class ADTRitualEffect
{
    public abstract void Effect(IEntityManager entMan, ADTRitualArgs args);
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class ADTRitualCheck
{
    public abstract bool Check(IEntityManager entMan, ADTRitualArgs args, out string? reason);
}
