using Content.Shared.ADT.Cytology.Components.Container;

namespace Content.Shared.ADT.Cytology;

[Serializable, ImplicitDataDefinitionForInheritors]
public abstract partial class CellModifier
{
    public virtual void OnAdd(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        // Literally do nothing
    }

    public virtual void OnRemove(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        // Literally do nothing
    }
}
