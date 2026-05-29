using Content.Shared.ADT.Bubblegum;
using Content.Shared.ADT.Bubblegum.Abilities;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum.Abilities;

public sealed class BubblegumSurroundSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumSurroundComponent, BubblegumSurroundActionEvent>(OnAction);
    }

    private void OnAction(Entity<BubblegumSurroundComponent> ent, ref BubblegumSurroundActionEvent args)
    {
        if (args.Handled)
            return;

        var target = _transform.ToMapCoordinates(args.Target);
        if (target.MapId == MapId.Nullspace)
            return;

        args.Handled = true;
        StartSurround(ent, target);
    }

    public void StartSurround(Entity<BubblegumSurroundComponent> ent, MapCoordinates target, EntityUid? targetEntity = null)
    {
        if (ent.Comp.Waves <= 0 || ent.Comp.HallucinationsPerWave <= 0)
            return;

        var pending = EnsureComp<BubblegumPendingWavesComponent>(ent);
        var now = _timing.CurTime;
        for (var wave = 0; wave < ent.Comp.Waves; wave++)
        {
            pending.Queue.Add(new PendingWave
            {
                ExecuteAt = now + TimeSpan.FromSeconds(wave * ent.Comp.WaveDelay),
                Target = target,
                TargetEntity = targetEntity,
                Count = ent.Comp.HallucinationsPerWave,
                Radius = ent.Comp.Radius,
                Delay = ent.Comp.SelfChargeDelay,
                Speed = ent.Comp.ChargeSpeed,
                HallucinationProto = ent.Comp.HallucinationPrototype,
                TelegraphProto = ent.Comp.TelegraphPrototype,
                BossCharges = true
            });
        }
        Dirty(ent.Owner, pending);
    }
}
