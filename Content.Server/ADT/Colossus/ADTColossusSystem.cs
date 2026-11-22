using System.Numerics;
using Content.Server.ADT.Hierophant;
using Content.Server.ADT.Salvage.Systems;
using Content.Shared.ADT.Colossus;
using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.ADT.Colossus;

public sealed class ADTColossusSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private HierophantAttacksSystem _hierophantAttacks = default!;
    [Dependency] private HierophantSystem _hierophant = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const int HierophantLinkRange = 20;

    private readonly HashSet<Entity<ActorComponent>> _actors = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTColossusComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ADTColossusComponent, ProjectileReflectAttemptEvent>(OnProjectileReflectAttempt);
        SubscribeLocalEvent<ADTColossusComponent, HitscanRaycastStrikeEvent>(OnHitscanStrike);
        SubscribeLocalEvent<ADTColossusComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<ADTColossusComponent, ADTMegafaunaDevourEvent>(OnDevour);
        SubscribeLocalEvent<ADTColossusComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnMobStateChanged(Entity<ADTColossusComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var coords = _transform.GetMoverCoordinates(ent);

        _megafauna.ClearSchedule(ent);
        _popup.PopupCoordinates(Loc.GetString("adt-colossus-death", ("colossus", ent.Owner)), coords, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.DeathSound, coords);

        QueueDel(ent);
    }

    private void OnProjectileReflectAttempt(Entity<ADTColossusComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        SpawnShield(ent, _transform.GetMapCoordinates(args.ProjUid));
    }

    private void OnHitscanStrike(Entity<ADTColossusComponent> ent, ref HitscanRaycastStrikeEvent args)
    {
        var center = _transform.GetMapCoordinates(ent);
        var shooter = args.Data.Shooter ?? args.Data.Gun;
        var origin = _transform.GetMapCoordinates(shooter);

        if (origin.MapId != center.MapId || args.Data.ShotDirection.LengthSquared() < 0.0001f)
        {
            SpawnShield(ent, center);
            return;
        }

        var direction = Vector2.Normalize(args.Data.ShotDirection);
        var along = Vector2.Dot(center.Position - origin.Position, direction);

        if (along <= 0f)
        {
            SpawnShield(ent, center);
            return;
        }

        var closest = origin.Position + direction * along;
        var radius = ent.Comp.ShieldHitRadius;
        var back = MathF.Sqrt(MathF.Max(radius * radius - (center.Position - closest).LengthSquared(), 0f));
        var hit = origin.Position + direction * MathF.Max(along - back, 0f);

        SpawnShield(ent, new MapCoordinates(hit, center.MapId));
    }

    private void SpawnShield(Entity<ADTColossusComponent> ent, MapCoordinates hit)
    {
        if (_mobState.IsDead(ent) || hit.MapId == MapId.Nullspace)
            return;

        var local = Vector2.Transform(hit.Position, _transform.GetInvWorldMatrix(ent));
        SpawnAttachedTo(ent.Comp.ShieldProto, new EntityCoordinates(ent, local));
    }

    private void OnDevour(Entity<ADTColossusComponent> ent, ref ADTMegafaunaDevourEvent args)
    {
        args.Handled = true;

        var target = args.Target;
        var coords = _transform.GetMoverCoordinates(target);

        _popup.PopupCoordinates(
            Loc.GetString("adt-colossus-disintegrate", ("colossus", ent.Owner), ("target", target)),
            coords,
            PopupType.LargeCaution);

        _megafauna.Disintegrate(target, ent.Comp.RemainsProto);
    }

    private void OnRefreshSpeed(Entity<ADTColossusComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Cheesing)
            args.ModifySpeed(ent.Comp.CheeseSpeedMultiplier, ent.Comp.CheeseSpeedMultiplier);
    }

    private void OnDamageDealt(Entity<ADTColossusComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= FixedPoint2.Zero)
            return;

        var colossusCoords = _transform.GetMapCoordinates(ent);

        if (colossusCoords.MapId == MapId.Nullspace)
            return;

        var query = EntityQueryEnumerator<HierophantComponent>();

        while (query.MoveNext(out var uid, out var hierophant))
        {
            if (hierophant.Enraged || HasComp<ADTColossusHierophantUnlinkedComponent>(uid) || _mobState.IsDead(uid))
                continue;

            var hierophantCoords = _transform.GetMapCoordinates(uid);

            if (ADTMegafaunaGeometry.ChebyshevDistance(colossusCoords, hierophantCoords) > HierophantLinkRange)
                continue;

            if (FindPlayerNear(hierophantCoords) is not { } victim)
                continue;

            EnrageHierophant((uid, hierophant), victim);
        }
    }

    private EntityUid? FindPlayerNear(MapCoordinates center)
    {
        _actors.Clear();
        _lookup.GetEntitiesInRange(center, HierophantLinkRange * MathF.Sqrt(2f) + 0.5f, _actors);

        foreach (var actor in _actors)
        {
            if (!HasComp<MobStateComponent>(actor))
                continue;

            if (ADTMegafaunaGeometry.ChebyshevDistance(center, _transform.GetMapCoordinates(actor)) > HierophantLinkRange)
                continue;

            return actor.Owner;
        }

        return null;
    }

    private void EnrageHierophant(Entity<HierophantComponent> hierophant, EntityUid victim)
    {
        EnsureComp<ADTColossusHierophantUnlinkedComponent>(hierophant);

        hierophant.Comp.Enraged = true;
        _speed.RefreshMovementSpeedModifiers(hierophant.Owner);
        _hierophant.CalculateRage(hierophant);
        _hierophantAttacks.ArenaTrap(hierophant, victim, true);

        if (TryComp<MegafaunaComponent>(hierophant, out var megafauna))
            _megafauna.TryAggro((hierophant.Owner, megafauna), victim);
    }
}
