using Content.Shared.ADT.Mining.Components;
using Robust.Shared.Network;

namespace Content.Shared.ADT.Mining;

public abstract class SharedADTGibtoniteSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGibtoniteDepositComponent, ADTMineralDrillEvent>(OnDrill);
        SubscribeLocalEvent<ADTGibtoniteDepositComponent, ADTGatherAttemptEvent>(OnGatherAttempt);
    }

    private void OnDrill(Entity<ADTGibtoniteDepositComponent> ent, ref ADTMineralDrillEvent args)
    {
        args.Handled = true;

        if (_net.IsServer)
            Drilled(ent, args.User, args.ByExplosion);
    }

    private void OnGatherAttempt(Entity<ADTGibtoniteDepositComponent> ent, ref ADTGatherAttemptEvent args)
    {
        if (HasComp<ADTMineralComponent>(ent))
            return;

        args.Cancelled = true;

        if (_net.IsServer)
            Drilled(ent, args.Gatherer, false);
    }

    protected virtual void Drilled(Entity<ADTGibtoniteDepositComponent> ent, EntityUid? user, bool byExplosion)
    {
    }
}
