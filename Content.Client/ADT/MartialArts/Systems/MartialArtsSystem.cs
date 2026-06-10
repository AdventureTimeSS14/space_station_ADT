using Content.Shared.ADT.MartialArts;

namespace Content.Client.ADT.MartialArts;

public sealed class MartialArtsSystem : SharedMartialArtsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanPerformComboComponent, GetPerformedAttackTypesEvent>(OnGetAttackTypes);
    }

    private void OnGetAttackTypes(Entity<CanPerformComboComponent> ent, ref GetPerformedAttackTypesEvent args)
    {
        args.AttackTypes = ent.Comp.LastAttacks;
    }
}
