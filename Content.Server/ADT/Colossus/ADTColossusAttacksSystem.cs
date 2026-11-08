using System.Numerics;
using Content.Server.ADT.Salvage.Systems;
using Content.Shared.ADT.Colossus;
using Content.Shared.ADT.MartialArts;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.ADT.Colossus;

public sealed class ADTColossusAttacksSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTColossusComponent, ADTMegafaunaOpenFireEvent>(OnOpenFire);
        SubscribeLocalEvent<ADTColossusComponent, ADTColossusSpiralActionEvent>(OnSpiralAction);
        SubscribeLocalEvent<ADTColossusComponent, ADTColossusRandomShotsActionEvent>(OnRandomShotsAction);
        SubscribeLocalEvent<ADTColossusComponent, ADTColossusShotgunActionEvent>(OnShotgunAction);
        SubscribeLocalEvent<ADTColossusComponent, ADTColossusAlternatingActionEvent>(OnAlternatingAction);
    }

    private void OnOpenFire(Entity<ADTColossusComponent> ent, ref ADTMegafaunaOpenFireEvent args)
    {
        if (!TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            return;

        var anger = _megafauna.CalculateAnger((ent.Owner, ai));
        _megafauna.SetRangedCooldown((ent.Owner, ai), ent.Comp.OpenFireCooldown);

        if (IsTryingToCheese(ent, args.Target))
        {
            if (!ent.Comp.Cheesing)
                _megafauna.Say(ent, "adt-colossus-cannot-dodge", force: true);

            _megafauna.SetRangedCooldown((ent.Owner, ai), ent.Comp.CheeseCooldown);
            Telegraph(ent, null);
            DirShots(ent, ADTMegafaunaGeometry.AllDirs);
            SetCheesing(ent, true);
            return;
        }

        SetCheesing(ent, false);

        if (ent.Comp.FinalAvailable && _megafauna.GetHealth(ent) <= _megafauna.GetMaxHealth(ent) / ent.Comp.FinalHealthDivisor)
        {
            FinalAttack(ent, args.Target);
            ent.Comp.FinalAvailable = false;
            return;
        }

        if (_random.Prob(Math.Clamp(ent.Comp.SpiralChanceBase + anger / 100f, 0f, 1f)))
        {
            SelectSpiralAttack(ent);
            return;
        }

        if (_random.Prob(ent.Comp.RandomShotsChance))
        {
            RandomShots(ent, true);
            return;
        }

        if (_random.Prob(ent.Comp.BlastChance))
        {
            Blast(ent, TargetResolver(args.Target), true);
            return;
        }

        AlternatingDirShots(ent, true);
    }

    private void OnSpiralAction(Entity<ADTColossusComponent> ent, ref ADTColossusSpiralActionEvent args)
    {
        if (args.Handled || !TryStartPlayerAttack(ent))
            return;

        args.Handled = true;
        SelectSpiralAttack(ent);
    }

    private void OnRandomShotsAction(Entity<ADTColossusComponent> ent, ref ADTColossusRandomShotsActionEvent args)
    {
        if (args.Handled || !TryStartPlayerAttack(ent))
            return;

        args.Handled = true;
        RandomShots(ent, true);
    }

    private void OnShotgunAction(Entity<ADTColossusComponent> ent, ref ADTColossusShotgunActionEvent args)
    {
        if (args.Handled || !TryStartPlayerAttack(ent))
            return;

        args.Handled = true;

        var target = args.Target;
        Blast(ent, () => _transform.ToMapCoordinates(target), true);
    }

    private void OnAlternatingAction(Entity<ADTColossusComponent> ent, ref ADTColossusAlternatingActionEvent args)
    {
        if (args.Handled || !TryStartPlayerAttack(ent))
            return;

        args.Handled = true;
        AlternatingDirShots(ent, true);
    }

    private bool TryStartPlayerAttack(Entity<ADTColossusComponent> ent)
    {
        if (!TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            return false;

        Entity<ADTMegafaunaAIComponent> boss = (ent.Owner, ai);

        if (!_megafauna.IsRangedReady(boss))
            return false;

        _megafauna.CalculateAnger(boss);
        _megafauna.SetRangedCooldown(boss, ent.Comp.OpenFireCooldown);
        return true;
    }

    private bool IsTryingToCheese(Entity<ADTColossusComponent> ent, EntityUid target)
    {
        if (!TryComp<MartialArtsKnowledgeComponent>(target, out var knowledge) || knowledge.Blocked)
            return false;

        return ent.Comp.CheeseForms.Contains(knowledge.MartialArtsForm);
    }

    private void SetCheesing(Entity<ADTColossusComponent> ent, bool value)
    {
        ent.Comp.Cheesing = value;
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void SetCooldown(Entity<ADTColossusComponent> ent, TimeSpan cooldown)
    {
        if (TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            _megafauna.SetRangedCooldown((ent.Owner, ai), cooldown);
    }

    private void Telegraph(Entity<ADTColossusComponent> ent, string? mode)
    {
        _megafauna.FlashAndShakeScreens(
            ent,
            ent.Comp.TelegraphRange,
            ent.Comp.TelegraphColor,
            ent.Comp.TelegraphFlashDuration,
            ent.Comp.TelegraphShakeStrength);

        _audio.PlayPvs(ent.Comp.TelegraphSound, ent);

        if (mode != null)
            _megafauna.Say(ent, mode, force: true);
    }

    private void SelectSpiralAttack(Entity<ADTColossusComponent> ent)
    {
        var doubleSpiral = _megafauna.GetHealth(ent) < _megafauna.GetMaxHealth(ent) * ent.Comp.DoubleSpiralHealthFraction;

        _megafauna.Say(ent, doubleSpiral ? "adt-colossus-die" : "adt-colossus-judgement", force: true);
        Telegraph(ent, null);

        _megafauna.Schedule(ent, ent.Comp.SpiralDelay, () =>
        {
            if (doubleSpiral)
            {
                SpiralShoot(ent, false);
                SpiralShoot(ent, true);
                return;
            }

            SpiralShoot(ent, _random.Prob(0.5f));
        });
    }

    private void SpiralShoot(Entity<ADTColossusComponent> ent, bool negative)
    {
        BeginAttackAnimation(ent);

        var counter = ent.Comp.SpiralCounterStart;

        for (var i = 0; i < ent.Comp.SpiralShots; i++)
        {
            if (negative)
                counter--;
            else
                counter++;

            if (counter > ent.Comp.SpiralCounterMax)
                counter = 1;

            if (counter < 1)
                counter = ent.Comp.SpiralCounterMax;

            var angle = counter * ent.Comp.SpiralAngleStep;

            _megafauna.Schedule(ent, ent.Comp.SpiralStepDelay * i, () =>
            {
                ShootAngle(ent, angle);
                _audio.PlayPvs(ent.Comp.ShotSound, ent, AudioParams.Default.WithVolume(ent.Comp.SpiralShotVolume));
            });
        }

        _megafauna.Schedule(ent, ent.Comp.SpiralStepDelay * ent.Comp.SpiralShots, () => EndAttackAnimation(ent));
    }

    private void RandomShots(Entity<ADTColossusComponent> ent, bool telegraphing)
    {
        SetCooldown(ent, ent.Comp.RandomShotsCooldown);

        if (!telegraphing)
        {
            FireRandomShots(ent);
            return;
        }

        Telegraph(ent, "adt-colossus-wrath");
        _megafauna.Schedule(ent, ent.Comp.TelegraphDelay, () => FireRandomShots(ent));
    }

    private void FireRandomShots(Entity<ADTColossusComponent> ent)
    {
        FireAtRandomTiles(ent, ent.Comp.RandomShotChance, ent.Comp.RandomShotsExtraRange);
    }

    private void FireAtRandomTiles(Entity<ADTColossusComponent> ent, float chance, float extraRange)
    {
        if (GetOrigin(ent) is not { } origin)
            return;

        _audio.PlayPvs(ent.Comp.ShotSound, ent, AudioParams.Default
            .WithVolume(ent.Comp.VolleyVolume)
            .WithMaxDistance(SharedAudioSystem.DefaultSoundRange + extraRange));

        foreach (var tile in ADTMegafaunaGeometry.RangeTiles(origin, ent.Comp.RandomShotsRange))
        {
            if (tile.Position == origin.Position)
                continue;

            if (!_random.Prob(chance))
                continue;

            ShootAt(ent, tile);
        }
    }

    private void Blast(Entity<ADTColossusComponent> ent, Func<MapCoordinates?> resolveTarget, bool telegraphing, bool setCooldown = true)
    {
        if (setCooldown)
            SetCooldown(ent, ent.Comp.BlastCooldown);

        var delay = ent.Comp.BlastNoTelegraphDelay;

        if (telegraphing)
        {
            Telegraph(ent, "adt-colossus-retribution");
            delay = ent.Comp.TelegraphDelay;
        }

        _megafauna.Schedule(ent, delay, () => FireBlast(ent, resolveTarget()));
    }

    private void FireBlast(Entity<ADTColossusComponent> ent, MapCoordinates? target)
    {
        if (target is not { } targetCoords || GetOrigin(ent) is not { } origin)
            return;

        if (targetCoords.MapId != origin.MapId)
            return;

        var targetTile = SnapToTile(targetCoords);

        _audio.PlayPvs(ent.Comp.ShotSound, ent, AudioParams.Default
            .WithVolume(ent.Comp.VolleyVolume)
            .WithMaxDistance(SharedAudioSystem.DefaultSoundRange + ent.Comp.VolleyExtraRange));

        var delta = targetTile.Position - origin.Position;
        var baseAngle = delta.LengthSquared() < 0.01f
            ? 90f
            : ADTMegafaunaGeometry.VectorToByondAngle(delta);

        foreach (var offset in ent.Comp.ShotgunAngles)
        {
            ShootAngle(ent, baseAngle + offset);
        }
    }

    private void AlternatingDirShots(Entity<ADTColossusComponent> ent, bool telegraphing)
    {
        var rage = ent.Comp.AlternatingDelay;
        var at = TimeSpan.Zero;

        if (telegraphing)
        {
            SetCooldown(ent, ent.Comp.AlternatingCooldown);
            Telegraph(ent, "adt-colossus-lament");
            at = ent.Comp.TelegraphDelay;
        }

        _megafauna.Schedule(ent, at, () => DirShots(ent, ADTMegafaunaGeometry.Diagonals));
        at += rage;
        _megafauna.Schedule(ent, at, () => DirShots(ent, ADTMegafaunaGeometry.Cardinals));
        at += rage;
        _megafauna.Schedule(ent, at, () => DirShots(ent, ADTMegafaunaGeometry.Diagonals));
        at += rage;
        _megafauna.Schedule(ent, at, () => DirShots(ent, ADTMegafaunaGeometry.Cardinals));
    }

    private void DirShots(Entity<ADTColossusComponent> ent, Direction[] dirs)
    {
        if (GetOrigin(ent) is not { } origin)
            return;

        _audio.PlayPvs(ent.Comp.ShotSound, ent, AudioParams.Default
            .WithVolume(ent.Comp.VolleyVolume)
            .WithMaxDistance(SharedAudioSystem.DefaultSoundRange + ent.Comp.VolleyExtraRange));

        foreach (var dir in dirs)
        {
            ShootAt(ent, ADTMegafaunaGeometry.Offset(origin, dir, 1));
        }
    }

    private void FinalAttack(Entity<ADTColossusComponent> ent, EntityUid target)
    {
        BeginAttackAnimation(ent);
        _megafauna.Say(ent, "adt-colossus-perish", force: true);
        Telegraph(ent, null);

        var resolveTarget = TargetResolver(target);
        var at = ent.Comp.FinalDelay;
        var counter = ent.Comp.FinalCounterStart;

        for (var i = 0; i < ent.Comp.FinalBlasts; i++)
        {
            if (counter > ent.Comp.FinalCounterBlastThreshold)
            {
                _megafauna.Schedule(ent, at, () => Telegraph(ent, null));
                at += ent.Comp.BlastNoTelegraphDelay;
                _megafauna.Schedule(ent, at, () => FireBlast(ent, resolveTarget()));
            }

            if (counter > 1)
                counter--;

            var chance = Math.Min(counter, 2) / 100f;
            _megafauna.Schedule(ent, at, () => FireAtRandomTiles(ent, chance, ent.Comp.RandomShotsExtraRange));

            at += TimeSpan.FromSeconds(counter * 0.1 + 0.2);
        }

        for (var i = 0; i < ent.Comp.FinalRandomShotsWaves; i++)
        {
            _megafauna.Schedule(ent, at, () =>
            {
                Telegraph(ent, null);
                FireRandomShots(ent);
            });

            counter += ent.Comp.FinalCounterRandomShotsStep;
            at += TimeSpan.FromSeconds(counter * 0.1);
        }

        for (var i = 0; i < ent.Comp.FinalDirShotsWaves; i++)
        {
            _megafauna.Schedule(ent, at, () =>
            {
                Telegraph(ent, null);
                DirShots(ent, ADTMegafaunaGeometry.AllDirs);
            });

            at += ent.Comp.FinalDirShotsDelay;
        }

        SetCooldown(ent, at > ent.Comp.FinalCooldown ? at : ent.Comp.FinalCooldown);

        _megafauna.Schedule(ent, at, () =>
        {
            EndAttackAnimation(ent);
            SetCooldown(ent, ent.Comp.FinalEndCooldown);
        });
    }

    private Func<MapCoordinates?> TargetResolver(EntityUid target)
    {
        return () =>
        {
            if (TerminatingOrDeleted(target))
                return null;

            return _transform.GetMapCoordinates(target);
        };
    }

    private void ShootAngle(Entity<ADTColossusComponent> ent, float byondAngle)
    {
        if (GetOrigin(ent) is not { } origin)
            return;

        Fire(ent, origin, ADTMegafaunaGeometry.ByondAngleToVector(byondAngle));
    }

    private void ShootAt(Entity<ADTColossusComponent> ent, MapCoordinates target)
    {
        if (GetOrigin(ent) is not { } origin || origin.MapId != target.MapId)
            return;

        var direction = target.Position - origin.Position;

        if (direction.LengthSquared() < 0.01f)
            return;

        Fire(ent, origin, direction);
    }

    private void Fire(Entity<ADTColossusComponent> ent, MapCoordinates origin, Vector2 direction)
    {
        var bolt = Spawn(ent.Comp.BoltProto, origin);

        if (TryComp<TimedDespawnComponent>(bolt, out var despawn) && ent.Comp.BoltSpeed > 0f)
            despawn.Lifetime = ent.Comp.BoltRange / ent.Comp.BoltSpeed;

        _gun.ShootProjectile(bolt, direction, Vector2.Zero, ent.Owner, ent.Owner, ent.Comp.BoltSpeed);
    }

    private MapCoordinates? GetOrigin(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return null;

        var coords = _transform.GetMoverCoordinates(uid).SnapToGrid(EntityManager);
        var map = _transform.ToMapCoordinates(coords);

        if (map.MapId == MapId.Nullspace)
            return null;

        return map;
    }

    private MapCoordinates SnapToTile(MapCoordinates coords)
    {
        var snapped = _transform.ToCoordinates(coords).SnapToGrid(EntityManager);
        return _transform.ToMapCoordinates(snapped);
    }

    private void BeginAttackAnimation(Entity<ADTColossusComponent> ent)
    {
        ent.Comp.AttackingSequences++;
        _appearance.SetData(ent, ADTColossusVisuals.Attacking, true);
    }

    private void EndAttackAnimation(Entity<ADTColossusComponent> ent)
    {
        ent.Comp.AttackingSequences = Math.Max(0, ent.Comp.AttackingSequences - 1);

        if (ent.Comp.AttackingSequences == 0)
            _appearance.SetData(ent, ADTColossusVisuals.Attacking, false);
    }
}
