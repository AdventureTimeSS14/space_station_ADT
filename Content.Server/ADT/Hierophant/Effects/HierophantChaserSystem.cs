using System.Numerics;
using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.Hierophant.Effects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Hierophant.Effects;

public sealed class HierophantChaserSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly Direction[] Cardinals =
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West, // IDK SHITCODE REWORK SOON MAYBE? 
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantChaserComponent, MapInitEvent>(OnChaserInit);
    }

    private void OnChaserInit(Entity<HierophantChaserComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextStepAt = _timing.CurTime + TimeSpan.FromSeconds(0.1);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<HierophantChaserComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextStepAt)
                continue;

            comp.NextStepAt = now + TimeSpan.FromSeconds(comp.Speed);
            Step((uid, comp));
        }
    }

    private void Step(Entity<HierophantChaserComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.Target == null || TerminatingOrDeleted(comp.Target.Value))
            return;

        var targetCoords = _transform.GetMapCoordinates(comp.Target.Value);
        var ourCoords = _transform.GetMapCoordinates(ent.Owner);

        if (targetCoords.MapId != ourCoords.MapId)
            return;

        if (comp.Moving <= 0)
        {
            comp.MorePreviouserMovingDir = comp.PreviousMovingDir;
            comp.PreviousMovingDir = comp.MovingDir;
            comp.MovingDir = GetTargetDir(comp, ourCoords, targetCoords);

            var standardTargetDir = GetCardinalDir(ourCoords, targetCoords);

            if (standardTargetDir == null
                || (standardTargetDir != comp.PreviousMovingDir && standardTargetDir == comp.MorePreviouserMovingDir))
            {
                comp.Moving = 1;
            }
            else
            {
                comp.Moving = comp.StandardMovingBeforeRecalc;
            }
        }

        var destination = _transform.GetMoverCoordinates(ent.Owner);

        for (var i = 0; i < comp.TilesPerStep; i++)
        {
            destination = destination.Offset(comp.MovingDir.ToVec());
        }

        _transform.SetCoordinates(ent.Owner, destination);
        MakeBlast(ent);
        comp.Moving--;
    }

    private Direction GetTargetDir(HierophantChaserComponent comp, MapCoordinates from, MapCoordinates to)
    {
        var dir = GetCardinalDir(from, to);

        if (dir == null || (dir != comp.PreviousMovingDir && dir == comp.MorePreviouserMovingDir))
        {
            var choices = new List<Direction>(Cardinals);

            if (comp.MorePreviouserMovingDir != null)
                choices.Remove(comp.MorePreviouserMovingDir.Value);

            return _random.Pick(choices);
        }

        return dir.Value;
    }

    private Direction? GetCardinalDir(MapCoordinates from, MapCoordinates to)
    {
        var delta = to.Position - from.Position;

        if (delta.LengthSquared() < 0.1f)
            return null;

        if (MathF.Abs(delta.X) > MathF.Abs(delta.Y))
            return delta.X > 0 ? Direction.East : Direction.West;

        return delta.Y > 0 ? Direction.North : Direction.South;
    }

    private void MakeBlast(Entity<HierophantChaserComponent> ent)
    {
        if (!TryComp<HierophantComponent>(ent.Comp.Caster, out var boss))
        {
            SpawnBlast(ent, "ADTHierophantBlast");
            return;
        }

        SpawnBlast(ent, boss.BlastProto);
    }

    private void SpawnBlast(Entity<HierophantChaserComponent> ent, string proto)
    {
        var blast = Spawn(proto, _transform.GetMoverCoordinates(ent.Owner));

        if (!TryComp<HierophantBlastComponent>(blast, out var blastComp))
            return;

        blastComp.Caster = ent.Comp.Caster;
        blastComp.Damage = ent.Comp.Damage;
        blastComp.MonsterDamageBoost = ent.Comp.MonsterDamageBoost;
        blastComp.FriendlyFireCheck = ent.Comp.FriendlyFireCheck;
    }
}
