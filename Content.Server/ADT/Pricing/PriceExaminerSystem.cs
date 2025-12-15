using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;

namespace Content.Server.ADT.Pricing;

public sealed class PriceExaminerSystem : EntitySystem
{
    [Dependency] private readonly PricingSystem _pricing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaticPriceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<StaticPriceComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<PriceExaminerComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("price-examiner-cost", ("target", Identity.Name(ent.Owner, EntityManager)), ("cost", _pricing.GetPrice(ent.Owner))), 5);
    }
}
