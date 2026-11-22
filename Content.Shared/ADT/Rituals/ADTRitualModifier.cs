namespace Content.Shared.ADT.Rituals;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ADTRitualModifier
{
    public abstract void Apply(IEntityManager entMan, ADTRitualArgs args, ref float fail, ref float disaster);
}
