using Content.Shared.ADT.Wizard.FadingTimedDespawn;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob Wizard.Traps
// only WizardTrap + DamageTrap kept, rest unused

public abstract class SharedWizardTrapsSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private   readonly SharedTransformSystem _transform = default!;
    [Dependency] private   readonly SharedPopupSystem _popup = default!;
    [Dependency] private   readonly SharedMindSystem _mind = default!;
    [Dependency] private   readonly SharedStunSystem _stun = default!;
    [Dependency] private   readonly DamageableSystem _damageable = default!;
    [Dependency] private   readonly SharedAudioSystem _audio = default!;
    [Dependency] private   readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private   readonly INetManager _net = default!;
    [Dependency] private   readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WizardTrapComponent, ExamineAttemptEvent>(OnExamineAttempt);
        SubscribeLocalEvent<WizardTrapComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<WizardTrapComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<WizardTrapComponent, StartCollideEvent>(OnStartCollide);

        SubscribeLocalEvent<DamageTrapComponent, TrapTriggeredEvent>(OnDamageTriggered);
    }

    private void OnDamageTriggered(Entity<DamageTrapComponent> ent, ref TrapTriggeredEvent args)
    {
        _damageable.TryChangeDamage(args.Victim, ent.Comp.Damage, true);
        if (_net.IsServer && ent.Comp.SpawnedEntity is { } toSpawn)
            Spawn(toSpawn, _transform.GetMapCoordinates(ent));
    }

    private void OnStartCollide(Entity<WizardTrapComponent> ent, ref StartCollideEvent args)
    {
        var (uid, comp) = ent;

        if (comp.Triggered)
            return;

        if (_net.IsClient && _player.LocalEntity != args.OtherEntity)
            return;

        if (HasComp<GodmodeComponent>(args.OtherEntity) || HasComp<IceCubeComponent>(args.OtherEntity))
            return;

        if (IsEntityMindIgnored(args.OtherEntity, comp))
            return;

        if (!comp.Silent)
        {
            _popup.PopupClient(Loc.GetString("trap-triggered-message", ("trap", uid)),
                args.OtherEntity,
                PopupType.LargeCaution);
        }

        comp.Triggered = true;
        comp.Charges--;
        Dirty(ent);

        if (HasComp<FadingTimedDespawnComponent>(uid))
            return;

        if (comp.StunTime > TimeSpan.Zero)
            _stun.TryUpdateParalyzeDuration(args.OtherEntity, comp.StunTime);

        RaiseLocalEvent(uid, new TrapTriggeredEvent(args.OtherEntity));

        if (_net.IsServer && comp.Sparks)
            Spawn("EffectSparks", _transform.GetMapCoordinates(uid));

        _audio.PlayPredicted(comp.TriggerSound, args.OtherEntity, args.OtherEntity);

        if (_net.IsClient)
            return;

        if (comp.Effect != null)
            Spawn(comp.Effect.Value, _transform.GetMapCoordinates(uid));

        if (comp.Charges <= 0)
        {
            QueueDel(uid);
            return;
        }

        Timer.Spawn(comp.TimeBetweenTriggers,
            () =>
            {
                if (!TryComp(uid, out WizardTrapComponent? trap))
                    return;

                trap.Triggered = false;
                Dirty(uid, trap);
            });
    }

    private void OnPreventCollide(Entity<WizardTrapComponent> ent, ref PreventCollideEvent args)
    {
        if (IsEntityMindIgnored(args.OtherEntity, ent.Comp))
            args.Cancelled = true;
    }

    private void OnExamine(Entity<WizardTrapComponent> ent, ref ExaminedEvent args)
    {
        var (uid, comp) = ent;

        if (!comp.CanReveal)
            return;

        if (TerminatingOrDeleted(uid))
            return;

        if (HasComp<FadingTimedDespawnComponent>(uid))
            return;

        if (IsEntityMindIgnored(args.Examiner, comp))
            return;

        if (!_transform.InRange(uid, args.Examiner, comp.ExamineRange))
            return;

        _popup.PopupClient(Loc.GetString("trap-revealed-message", ("trap", uid)), args.Examiner, PopupType.Medium);
        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("trap-flare-message", ("trap", uid)), uid, PopupType.MediumCaution);

        Appearance.SetData(uid, TrapVisuals.Alpha, 0.8f);

        var fading = EnsureComp<FadingTimedDespawnComponent>(uid);
        fading.Lifetime = 0.5f;
        fading.FadeOutTime = 1f;
        Dirty(uid, fading);
    }

    private void OnExamineAttempt(Entity<WizardTrapComponent> ent, ref ExamineAttemptEvent args)
    {
        var (uid, comp) = ent;

        if (TerminatingOrDeleted(uid))
            return;

        if (IsEntityMindIgnored(args.Examiner, comp))
            return;

        if (!comp.CanReveal)
            args.Cancel();
        else if ((TryComp(args.Examiner, out BlindableComponent? blindable) && blindable.IsBlind) ||
                 HasComp<PermanentBlindnessComponent>(args.Examiner))
            args.Cancel();
        else if (!_transform.InRange(uid, args.Examiner, comp.ExamineRange))
            args.Cancel();
    }

    private bool IsEntityMindIgnored(EntityUid user, WizardTrapComponent trap)
    {
        if (HasComp<GhostComponent>(user) || HasComp<SpectralComponent>(user) || !HasComp<MobStateComponent>(user))
            return true;

        if (trap.TargetedEntityWhitelist != null && !_whitelist.IsWhitelistPass(trap.TargetedEntityWhitelist, user))
            return true;

        if (_whitelist.IsWhitelistPass(trap.IgnoredEntityWhitelist, user))
            return true;

        return _mind.TryGetMind(user, out var mind, out _) && trap.IgnoredMinds.Contains(mind);
    }
}

public sealed class TrapTriggeredEvent(EntityUid victim) : EntityEventArgs
{
    public EntityUid Victim = victim;
}
