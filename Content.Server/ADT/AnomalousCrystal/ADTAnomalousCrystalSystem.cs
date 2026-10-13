using Content.Shared.Damage.Components;
using Content.Shared.Explosion;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Speech;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.AnomalousCrystal;

public sealed partial class ADTAnomalousCrystalSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAnomalousCrystalComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTAnomalousCrystalComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ADTAnomalousCrystalComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTAnomalousCrystalComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<ADTAnomalousCrystalComponent, ProjectileReflectAttemptEvent>(OnProjectileHit);
        SubscribeLocalEvent<ADTAnomalousCrystalComponent, HitScanReflectAttemptEvent>(OnHitscanHit);
        SubscribeLocalEvent<ADTAnomalousCrystalComponent, GetExplosionResistanceEvent>(OnExplosion);
        SubscribeLocalEvent<ADTAnomalousCrystalComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<ADTAnomalousCrystalComponent, ListenEvent>(OnListen);

        InitializeEffects();
    }

    private void OnMapInit(Entity<ADTAnomalousCrystalComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Methods.Count > 0)
            ent.Comp.Method = _random.Pick(ent.Comp.Methods);
    }

    private void OnInteractHand(Entity<ADTAnomalousCrystalComponent> ent, ref InteractHandEvent args)
    {
        if (TryActivate(ent, args.User, ADTCrystalActivation.Touch))
            args.Handled = true;
    }

    private void OnInteractUsing(Entity<ADTAnomalousCrystalComponent> ent, ref InteractUsingEvent args)
    {
        if (TryActivate(ent, args.User, ADTCrystalActivation.Weapon))
            args.Handled = true;
    }

    private void OnAttacked(Entity<ADTAnomalousCrystalComponent> ent, ref AttackedEvent args)
    {
        TryActivate(ent, args.User, ADTCrystalActivation.Weapon);
    }

    private void OnProjectileHit(Entity<ADTAnomalousCrystalComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        var method = ADTCrystalActivation.Bullet;

        if (TryComp<ReflectiveComponent>(args.ProjUid, out var reflective) && (reflective.Reflective & ReflectType.Energy) != 0)
        {
            method = args.Component.Damage.Empty && HasComp<StaminaDamageOnCollideComponent>(args.ProjUid)
                ? ADTCrystalActivation.Energy
                : ADTCrystalActivation.Laser;
        }

        TryActivate(ent, args.Component.Shooter, method);
    }

    private void OnHitscanHit(Entity<ADTAnomalousCrystalComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        var method = (args.Reflective & ReflectType.Energy) != 0
            ? ADTCrystalActivation.Laser
            : ADTCrystalActivation.Bullet;

        TryActivate(ent, args.Shooter, method);
    }

    private void OnExplosion(Entity<ADTAnomalousCrystalComponent> ent, ref GetExplosionResistanceEvent args)
    {
        TryActivate(ent, null, ADTCrystalActivation.Bomb);
    }

    private void OnCollide(Entity<ADTAnomalousCrystalComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<MobStateComponent>(args.OtherEntity))
            return;

        TryActivate(ent, args.OtherEntity, ADTCrystalActivation.MobBump);
    }

    private void OnListen(Entity<ADTAnomalousCrystalComponent> ent, ref ListenEvent args)
    {
        if (args.Message.Length == 0 || !HasComp<MobStateComponent>(args.Source))
            return;

        TryActivate(ent, args.Source, ADTCrystalActivation.Speech);
    }

    public bool TryActivate(Entity<ADTAnomalousCrystalComponent> ent, EntityUid? user, ADTCrystalActivation method)
    {
        if (method != ent.Comp.Method)
            return false;

        var now = _timing.CurTime;

        if (now < ent.Comp.NextUse)
            return false;

        ent.Comp.NextUse = now + ent.Comp.Cooldown;

        if (user != null)
            _audio.PlayPvs(ent.Comp.ActivationSound, user.Value);
        else
            _audio.PlayPvs(ent.Comp.ActivationSound, ent);

        var ev = new ADTCrystalActivatedEvent(user, method);
        RaiseLocalEvent(ent, ref ev);

        return true;
    }
}
