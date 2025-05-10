using Content.Shared.CombatMode.Pacification;
using Content.Shared.ADT.Cytology.Components.Container;
using JetBrains.Annotations;

namespace Content.Shared.ADT.Cytology.Modifiers;

[Serializable, UsedImplicitly]
public sealed partial class CellPacifismModifier : CellModifier
{
    public override void OnAdd(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        base.OnAdd(ent, cell, entityManager);
        entityManager.EnsureComponent<PacifiedComponent>(ent);
    }

    public override void OnRemove(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        base.OnRemove(ent, cell, entityManager);
        entityManager.RemoveComponent<PacifiedComponent>(ent);
    }
}
