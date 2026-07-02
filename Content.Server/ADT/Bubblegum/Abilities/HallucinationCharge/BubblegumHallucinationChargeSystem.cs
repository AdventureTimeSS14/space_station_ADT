using System.Numerics;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.ADT.Bubblegum.Abilities;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum.Abilities;

public sealed class BubblegumHallucinationChargeSystem : EntitySystem
{
    [Dependency] private readonly BubblegumChargeSystem _charge = default!;
    [Dependency] private readonly BubblegumTripleChargeSystem _tripleCharge = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float TravelBuffer = 0.7f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumHallucinationChargeComponent, BubblegumHallucinationChargeActionEvent>(OnAction);
    }

    private void OnAction(Entity<BubblegumHallucinationChargeComponent> ent, ref BubblegumHallucinationChargeActionEvent args)
    {
        if (args.Handled)
            return;

        var target = _transform.ToMapCoordinates(args.Target);
        if (target.MapId == MapId.Nullspace)
            return;

        args.Handled = true;
        StartHallucinationCharge(ent, target);
    }

    public void StartHallucinationCharge(Entity<BubblegumHallucinationChargeComponent> ent, MapCoordinates target, EntityUid? targetEntity = null)
    {
        var inSmash = TryComp<BubblegumComponent>(ent, out var bossComp) && bossComp.InSmashPhase;

        if (!inSmash || ent.Comp.SmashWaveDelays.Count == 0)
        {
            SpawnWave(ent.Owner, target, ent.Comp.HallucinationsNormal, ent.Comp.Radius, ent.Comp.NormalDelay,
                ent.Comp.ChargeSpeed, ent.Comp.HallucinationPrototype, ent.Comp.TelegraphPrototype);

            _charge.BeginCharge(ent.Owner, target, ent.Comp.NormalDelay, ent.Comp.ChargeSpeed,
                ent.Comp.TelegraphPrototype, trampleDamage: 30f, targetEntity: targetEntity);
            return;
        }

        var pending = EnsureComp<BubblegumPendingWavesComponent>(ent);
        var now = _timing.CurTime;
        var cumulative = 0f;
        for (var i = 0; i < ent.Comp.SmashWaveDelays.Count; i++)
        {
            var delay = ent.Comp.SmashWaveDelays[i];
            pending.Queue.Add(new PendingWave
            {
                ExecuteAt = now + TimeSpan.FromSeconds(cumulative),
                Target = target,
                TargetEntity = targetEntity,
                Count = ent.Comp.HallucinationsSmash,
                Radius = ent.Comp.Radius,
                Delay = delay,
                Speed = ent.Comp.ChargeSpeed,
                HallucinationProto = ent.Comp.HallucinationPrototype,
                TelegraphProto = ent.Comp.TelegraphPrototype,
                BossCharges = i != ent.Comp.SmashWaveDelays.Count - 1,
                TripleChargeAfter = i == ent.Comp.SmashWaveDelays.Count - 1
            });
            cumulative += delay + TravelBuffer;
        }
        Dirty(ent.Owner, pending);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BubblegumPendingWavesComponent>();
        while (query.MoveNext(out var uid, out var pending))
        {
            for (var i = pending.Queue.Count - 1; i >= 0; i--)
            {
                var wave = pending.Queue[i];
                if (now < wave.ExecuteAt)
                    continue;

                if (wave.TargetEntity is { } targetEntity
                    && !TerminatingOrDeleted(targetEntity))
                {
                    var current = _transform.GetMapCoordinates(targetEntity);
                    if (current.MapId == wave.Target.MapId)
                        wave.Target = current;
                }

                SpawnWave(uid, wave.Target, wave.Count, wave.Radius, wave.Delay, wave.Speed,
                    wave.HallucinationProto, wave.TelegraphProto);

                if (wave.BossCharges)
                {
                    _charge.BeginCharge(uid, wave.Target, wave.Delay, wave.Speed,
                        wave.TelegraphProto, trampleDamage: 30f);
                }

                if (wave.TripleChargeAfter && TryComp<BubblegumTripleChargeComponent>(uid, out var tc))
                    _tripleCharge.StartNpcTripleCharge((uid, tc), wave.Target);

                pending.Queue.RemoveAt(i);
            }

            if (pending.Queue.Count == 0)
                RemCompDeferred<BubblegumPendingWavesComponent>(uid);
        }
    }

    private void SpawnWave(EntityUid summoner, MapCoordinates target, int count, float radius, float delay, float speed,
        string halluProto, string telegraphProto)
    {
        if (count <= 0)
            return;

        var startAngle = _random.NextDouble() * Math.Tau;
        var stepAngle = Math.Tau / count;

        for (var i = 0; i < count; i++)
        {
            var ang = startAngle + stepAngle * i;
            var spawnPos = target.Position + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radius;
            var spawnCoords = new MapCoordinates(spawnPos, target.MapId);

            var clone = Spawn(halluProto, spawnCoords);
            EnsureComp<BubblegumMinionComponent>(clone).Summoner = summoner;
            _charge.BeginCharge(clone, target, delay, speed, telegraphProto,
                trampleDamage: 15f, expireOnHit: true);
        }
    }
}
