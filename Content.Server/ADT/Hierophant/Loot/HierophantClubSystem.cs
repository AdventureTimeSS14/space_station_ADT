using Content.Server.ADT.Hierophant.Effects;
using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.Hierophant.Effects;
using Content.Shared.ADT.Hierophant.Loot;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Hierophant.Loot;

public sealed class HierophantClubSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantClubComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HierophantClubComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<HierophantClubComponent, HierophantVortexRecallActionEvent>(OnRecall);
        SubscribeLocalEvent<HierophantClubComponent, HierophantToggleFriendlyFireActionEvent>(OnToggleFriendlyFire);
        SubscribeLocalEvent<HierophantClubComponent, HierophantBeaconDoAfterEvent>(OnBeaconDoAfter);
        SubscribeLocalEvent<HierophantClubComponent, HierophantTeleportDoAfterEvent>(OnTeleportDoAfter);
    }

    private void OnAfterInteract(Entity<HierophantClubComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (_timing.CurTime < ent.Comp.NextUseAt)
            return;

        var coords = _transform.ToMapCoordinates(args.ClickLocation);

        if (coords.MapId == MapId.Nullspace)
            return;

        var userCoords = _transform.GetMapCoordinates(args.User);

        if (HierophantAttacksSystem.ChebyshevDistance(userCoords, coords) > ent.Comp.Range)
        {
            _popup.PopupEntity(Loc.GetString("adt-hierophant-club-out-of-range"), args.User, args.User);
            return;
        }

        args.Handled = true;

        if (args.Target is { } target
            && target != args.User
            && HasComp<MobStateComponent>(target)
            && !_mobState.IsDead(target)
            && _timing.CurTime >= ent.Comp.NextChaserAt)
        {
            ent.Comp.NextChaserAt = _timing.CurTime + ent.Comp.ChaserCooldown;
            ent.Comp.NextUseAt = _timing.CurTime + ClubCooldown(ent, args.User);
            FireChaser(ent, args.User, target);
            UpdateAppearance(ent);
            return;
        }

        ent.Comp.NextUseAt = _timing.CurTime + ClubCooldown(ent, args.User);
        CardinalBlasts(ent, args.User, coords);
        UpdateAppearance(ent);
    }

    private void OnMeleeHit(Entity<HierophantClubComponent> ent, ref MeleeHitEvent args)
    {
        if (_timing.CurTime < ent.Comp.NextUseAt)
            return;

        ent.Comp.NextUseAt = _timing.CurTime + ent.Comp.MeleeCooldown;

        var coords = _transform.GetMapCoordinates(args.User);

        if (args.HitEntities.Count > 0)
            coords = _transform.GetMapCoordinates(args.HitEntities[0]);

        _audio.PlayPvs(ent.Comp.TelegraphSound, _transform.ToCoordinates(coords));
        Spawn("ADTHierophantTelegraph", _transform.ToCoordinates(coords));

        foreach (var tile in HierophantAttacksSystem.RangeTiles(coords, 1))
        {
            SpawnBlast(ent, args.User, tile, ent.Comp.MeleeDamage, true);
        }

        UpdateAppearance(ent);
    }

    private void CardinalBlasts(Entity<HierophantClubComponent> ent, EntityUid user, MapCoordinates coords)
    {
        _audio.PlayPvs(ent.Comp.TelegraphSound, _transform.ToCoordinates(coords));
        Spawn("ADTHierophantTelegraphCardinal", _transform.ToCoordinates(coords));

        SpawnBlast(ent, user, coords, ent.Comp.CardinalDamage, false);

        var range = GetBlastRange(ent, user);

        foreach (var dir in HierophantAttacksSystem.Cardinals)
        {
            for (var i = 1; i <= range; i++)
            {
                var tile = new MapCoordinates(coords.Position + dir.ToVec() * i, coords.MapId);
                SpawnBlast(ent, user, tile, ent.Comp.CardinalDamage, false);
            }
        }
    }

    private void FireChaser(Entity<HierophantClubComponent> ent, EntityUid user, EntityUid target)
    {
        var chaser = Spawn("ADTHierophantChaser", _transform.GetMoverCoordinates(user));

        if (!TryComp<HierophantChaserComponent>(chaser, out var comp))
            return;

        comp.Caster = user;
        comp.Target = target;
        comp.Speed = GetChaserSpeed(ent, user);
        comp.Damage = ent.Comp.ChaserDamage;
        comp.MonsterDamageBoost = false;
        comp.FriendlyFireCheck = ent.Comp.FriendlyFireCheck;
    }

    private EntityUid SpawnBlast(
        Entity<HierophantClubComponent> ent,
        EntityUid user,
        MapCoordinates coords,
        float damage,
        bool monsterDamageBoost)
    {
        var blast = Spawn("ADTHierophantBlast", coords);

        if (TryComp<HierophantBlastComponent>(blast, out var comp))
        {
            comp.Caster = user;
            comp.Damage = damage;
            comp.MonsterDamageBoost = monsterDamageBoost;
            comp.FriendlyFireCheck = ent.Comp.FriendlyFireCheck;
        }

        return blast;
    }

    private void OnToggleFriendlyFire(Entity<HierophantClubComponent> ent, ref HierophantToggleFriendlyFireActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.FriendlyFireCheck = !ent.Comp.FriendlyFireCheck;

        _popup.PopupEntity(
            Loc.GetString(ent.Comp.FriendlyFireCheck
                ? "adt-hierophant-club-friendly-fire-off"
                : "adt-hierophant-club-friendly-fire-on"),
            args.Performer,
            args.Performer);
    }

    private void OnRecall(Entity<HierophantClubComponent> ent, ref HierophantVortexRecallActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Teleporting || _timing.CurTime < ent.Comp.NextUseAt)
            return;

        args.Handled = true;
        var user = args.Performer;

        if (ent.Comp.Beacon == null || TerminatingOrDeleted(ent.Comp.Beacon.Value))
        {
            _popup.PopupEntity(Loc.GetString("adt-hierophant-club-detaching"), user, user);

            _doAfter.TryStartDoAfter(new DoAfterArgs(
                EntityManager,
                user,
                ent.Comp.BeaconDetachTime,
                new HierophantBeaconDoAfterEvent(),
                ent.Owner,
                target: user,
                used: ent.Owner)
            {
                BreakOnMove = true,
                NeedHand = true,
            });

            return;
        }

        var beaconCoords = _transform.GetMapCoordinates(ent.Comp.Beacon.Value);
        var userCoords = _transform.GetMapCoordinates(user);

        if (HierophantAttacksSystem.ChebyshevDistance(beaconCoords, userCoords) <= ent.Comp.MinimumTeleportDistance)
        {
            _popup.PopupEntity(Loc.GetString("adt-hierophant-club-too-close"), user, user);
            return;
        }

        ent.Comp.Teleporting = true;
        _appearance.SetData(ent.Comp.Beacon.Value, HierophantBeaconVisuals.Charging, true);

        ClearTeleportEffects(ent);
        ent.Comp.TeleportEffects.Add(Spawn(ent.Comp.TelegraphEdgeProto, _transform.GetMoverCoordinates(user)));
        ent.Comp.TeleportEffects.Add(Spawn(ent.Comp.TelegraphEdgeProto, _transform.GetMoverCoordinates(ent.Comp.Beacon.Value)));

        _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.TeleportChannelTime,
            new HierophantTeleportDoAfterEvent(),
            ent.Owner,
            target: user,
            used: ent.Owner)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnBeaconDoAfter(Entity<HierophantClubComponent> ent, ref HierophantBeaconDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var coords = _transform.GetMoverCoordinates(args.User);
        _audio.PlayPvs(ent.Comp.BeaconSound, coords);
        Spawn("ADTHierophantTelegraphTeleport", coords);

        var beacon = Spawn(ent.Comp.BeaconProto, coords);
        ent.Comp.Beacon = beacon;

        if (TryComp<HierophantBeaconComponent>(beacon, out var beaconComp))
            beaconComp.Club = ent.Owner;

        _popup.PopupEntity(Loc.GetString("adt-hierophant-club-beacon-placed"), args.User, args.User);
        UpdateAppearance(ent);
    }

    private void OnTeleportDoAfter(Entity<HierophantClubComponent> ent, ref HierophantTeleportDoAfterEvent args)
    {
        ent.Comp.Teleporting = false;

        if (ent.Comp.Beacon != null && !TerminatingOrDeleted(ent.Comp.Beacon.Value))
            _appearance.SetData(ent.Comp.Beacon.Value, HierophantBeaconVisuals.Charging, false);

        ClearTeleportEffects(ent);

        if (args.Cancelled || args.Handled)
            return;

        if (ent.Comp.Beacon == null || TerminatingOrDeleted(ent.Comp.Beacon.Value))
            return;

        args.Handled = true;

        var destination = _transform.GetMapCoordinates(ent.Comp.Beacon.Value);
        var source = _transform.GetMapCoordinates(args.User);

        _audio.PlayPvs(ent.Comp.TeleportSound, _transform.ToCoordinates(destination));
        _audio.PlayPvs(ent.Comp.DepartSound, _transform.ToCoordinates(source));
        Spawn("ADTHierophantTelegraphTeleport", _transform.ToCoordinates(destination));
        Spawn("ADTHierophantTelegraphTeleport", _transform.ToCoordinates(source));

        foreach (var tile in HierophantAttacksSystem.RangeTiles(destination, 1))
        {
            SpawnBlast(ent, args.User, tile, 30f, false);
        }

        foreach (var tile in HierophantAttacksSystem.RangeTiles(source, 1))
        {
            SpawnBlast(ent, args.User, tile, 30f, false);
        }

        foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(source, 1.5f))
        {
            if (HasComp<HierophantComponent>(mob.Owner) && _mobState.IsDead(mob.Owner))
                continue;

            var mobCoords = _transform.GetMapCoordinates(mob.Owner);
            var offset = mobCoords.Position - source.Position;
            _transform.SetMapCoordinates(mob.Owner, new MapCoordinates(destination.Position + offset, destination.MapId));
        }

        UpdateAppearance(ent);
    }

    private void ClearTeleportEffects(Entity<HierophantClubComponent> ent)
    {
        foreach (var effect in ent.Comp.TeleportEffects)
        {
            if (!TerminatingOrDeleted(effect))
                QueueDel(effect);
        }

        ent.Comp.TeleportEffects.Clear();
    }

    private int GetBlastRange(Entity<HierophantClubComponent> ent, EntityUid user)
    {
        return Math.Max(ent.Comp.BlastRange - (int) MathF.Round(GetHealthPercent(user) * 10f), 0);
    }

    private float GetChaserSpeed(Entity<HierophantClubComponent> ent, EntityUid user)
    {
        return MathF.Max(ent.Comp.ChaserSpeed + GetHealthPercent(user) * 0.1f, 0.05f);
    }

    private TimeSpan ClubCooldown(Entity<HierophantClubComponent> ent, EntityUid user)
    {
        return ent.Comp.CooldownTime + TimeSpan.FromSeconds(GetHealthPercent(user) * 1.0);
    }

    private float GetHealthPercent(EntityUid user)
    {
        if (!TryComp<MobThresholdsComponent>(user, out var thresholds))
            return 1f;

        var maxHealth = 0f;
        foreach (var (damage, _) in thresholds.Thresholds)
        {
            if ((float) damage > maxHealth)
                maxHealth = (float) damage;
        }

        if (maxHealth <= 0f)
            return 1f;

        var taken = (float) _damageable.GetTotalDamage(user);
        return Math.Clamp((maxHealth - taken) / maxHealth, 0f, 1f);
    }

    private void UpdateAppearance(Entity<HierophantClubComponent> ent)
    {
        _appearance.SetData(ent.Owner, HierophantClubVisuals.HasBeacon,
            ent.Comp.Beacon != null && !TerminatingOrDeleted(ent.Comp.Beacon.Value));
    }
}
