using Content.Server.Chat.Systems;
using Content.Server.Electrocution;
using Content.Shared.ADT.ComponentalActions;
using Content.Shared.ADT.ComponentalActions.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stealth.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Server.ADT.ComponentalActions;

public sealed partial class ComponentalActionsSystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> GeneticGroup = "Genetic";

    private readonly HashSet<Entity<StatusEffectsComponent>> _pulseTargets = new();

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<TeleportActComponent, CompTeleportActionEvent>(OnTeleport);
        SubscribeLocalEvent<ProjectileActComponent, CompProjectileActionEvent>(OnProjectile);
        SubscribeLocalEvent<HealActComponent, CompHealActionEvent>(OnHeal);
        SubscribeLocalEvent<JumpActComponent, CompJumpActionEvent>(OnJump);
        SubscribeLocalEvent<StasisHealActComponent, CompStasisHealActionEvent>(OnStasisHeal);
        SubscribeLocalEvent<InvisibilityActComponent, CompInvisibilityActionEvent>(OnInvisibility);
        SubscribeLocalEvent<ElectrionPulseActComponent, CompElectrionPulseActionEvent>(OnElectrionPulse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StasisHealActComponent>();
        while (query.MoveNext(out var uid, out var stasis))
        {
            if (!stasis.Active)
                continue;

            var heal = new DamageSpecifier(_proto.Index(BruteGroup), stasis.BruteHeal)
                + new DamageSpecifier(_proto.Index(BurnGroup), stasis.OtherHeal)
                + new DamageSpecifier(_proto.Index(AirlossGroup), stasis.OtherHeal)
                + new DamageSpecifier(_proto.Index(ToxinGroup), stasis.OtherHeal)
                + new DamageSpecifier(_proto.Index(GeneticGroup), stasis.OtherHeal);

            _damageable.TryChangeDamage(uid, heal);
            _bloodstream.TryModifyBloodLevel(uid, stasis.BloodRestore);
            _bloodstream.TryModifyBleedAmount(uid, stasis.BleedReduce);
        }
    }

    private void OnTeleport(Entity<TeleportActComponent> ent, ref CompTeleportActionEvent args)
    {
        if (args.Handled || Transform(ent).MapID != _transform.GetMapId(args.Target))
            return;

        _transform.SetCoordinates(ent, args.Target);
        _transform.AttachToGridOrMap(ent);
        _audio.PlayPvs(ent.Comp.Sound, ent);
        args.Handled = true;
    }

    private void OnProjectile(Entity<ProjectileActComponent> ent, ref CompProjectileActionEvent args)
    {
        if (args.Handled)
            return;

        var from = _transform.GetMapCoordinates(ent);
        var to = _transform.ToMapCoordinates(args.Target);
        var direction = to.Position - from.Position;

        if (from.MapId != to.MapId || direction.LengthSquared() < 0.01f)
            return;

        var projectile = Spawn(ent.Comp.Prototype, from);
        _gun.ShootProjectile(projectile, direction, _physics.GetMapLinearVelocity(ent), ent.Owner, ent.Owner, ent.Comp.Speed);
        _audio.PlayPvs(ent.Comp.Sound, ent);
        args.Handled = true;
    }

    private void OnHeal(Entity<HealActComponent> ent, ref CompHealActionEvent args)
    {
        if (args.Handled)
            return;

        var heal = new DamageSpecifier(_proto.Index(BruteGroup), ent.Comp.BruteHeal)
            + new DamageSpecifier(_proto.Index(BurnGroup), ent.Comp.BurnHeal);

        _damageable.TryChangeDamage(ent.Owner, heal);
        _bloodstream.TryModifyBloodLevel(ent.Owner, ent.Comp.BloodRestore);
        _bloodstream.TryModifyBleedAmount(ent.Owner, ent.Comp.BleedReduce);
        _audio.PlayPvs(ent.Comp.Sound, ent);
        args.Handled = true;
    }

    private void OnJump(Entity<JumpActComponent> ent, ref CompJumpActionEvent args)
    {
        if (args.Handled || Transform(ent).MapID != _transform.GetMapId(args.Target))
            return;

        _throwing.TryThrow(ent, args.Target, ent.Comp.Strength);
        _audio.PlayPvs(ent.Comp.Sound, ent);
        args.Handled = true;
    }

    private void OnStasisHeal(Entity<StasisHealActComponent> ent, ref CompStasisHealActionEvent args)
    {
        if (args.Handled)
            return;

        var move = EnsureComp<MovementSpeedModifierComponent>(ent);

        if (!ent.Comp.Active)
        {
            ent.Comp.SavedWalkSpeed = move.BaseWalkSpeed;
            ent.Comp.SavedSprintSpeed = move.BaseSprintSpeed;
            _movementSpeed.ChangeBaseSpeed(ent, ent.Comp.Speed, ent.Comp.Speed, move.Acceleration, move);
        }
        else
        {
            _movementSpeed.ChangeBaseSpeed(ent, ent.Comp.SavedWalkSpeed, ent.Comp.SavedSprintSpeed, move.Acceleration, move);
        }

        ent.Comp.Active = !ent.Comp.Active;
        args.Handled = true;
    }

    private void OnInvisibility(Entity<InvisibilityActComponent> ent, ref CompInvisibilityActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.Active)
        {
            var stealth = EnsureComp<StealthComponent>(ent);
            var stealthOnMove = EnsureComp<StealthOnMoveComponent>(ent);
            stealthOnMove.PassiveVisibilityRate = ent.Comp.PassiveVisibilityRate;
            stealthOnMove.MovementVisibilityRate = ent.Comp.MovementVisibilityRate;
            stealth.MinVisibility = ent.Comp.MinVisibility;
            stealth.MaxVisibility = ent.Comp.MaxVisibility;
            _popup.PopupEntity(Loc.GetString("componental-actions-invisibility-on"), ent, ent);
        }
        else
        {
            RemCompDeferred<StealthComponent>(ent);
            RemCompDeferred<StealthOnMoveComponent>(ent);
            _popup.PopupEntity(Loc.GetString("componental-actions-invisibility-off"), ent, ent);
        }

        ent.Comp.Active = !ent.Comp.Active;
        args.Handled = true;
    }

    private void OnElectrionPulse(Entity<ElectrionPulseActComponent> ent, ref CompElectrionPulseActionEvent args)
    {
        if (args.Handled)
            return;

        _chat.TrySendInGameICMessage(ent, Loc.GetString(ent.Comp.Emote), InGameICChatType.Emote, ChatTransmitRange.Normal);

        if (!HasComp<TimedDespawnComponent>(ent))
        {
            var despawn = AddComp<TimedDespawnComponent>(ent);
            despawn.Lifetime = ent.Comp.DespawnTime;
            _audio.PlayPvs(ent.Comp.Sound, ent);
        }

        _pulseTargets.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.Range, _pulseTargets);

        foreach (var target in _pulseTargets)
        {
            if (target.Owner == ent.Owner || !HasComp<InputMoverComponent>(target))
                continue;

            _electrocution.TryDoElectrocution(target, null, ent.Comp.ShockDamage, ent.Comp.ShockTime, refresh: true, ignoreInsulation: true);
        }

        args.Handled = true;
    }
}
