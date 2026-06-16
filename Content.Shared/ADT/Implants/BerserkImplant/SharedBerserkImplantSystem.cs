using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Jittering;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Implants.BerserkImplant;

public abstract class SharedBerserkImplantSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivateBerserkImplantActionEvent>(OnActivate);

        SubscribeLocalEvent<BerserkImplantActiveComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<BerserkImplantActiveComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<BerserkImplantActiveComponent, BeforeStaminaDamageEvent>(OnStaminaDamageModify);
        SubscribeLocalEvent<BerserkImplantActiveComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnActivate(ActivateBerserkImplantActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<BerserkImplantActiveComponent>(args.Performer))
            return;

        args.Handled = true;

        var uid = args.Performer;
        var Berserk = EnsureComp<BerserkImplantActiveComponent>(uid);
        Berserk.EndTime = Timing.CurTime + TimeSpan.FromSeconds(Berserk.Duration);

        _jitter.DoJitter(uid, TimeSpan.FromSeconds(3), true);
    }

    private void OnDamageModify(Entity<BerserkImplantActiveComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.DamageModifier);
    }

    private void OnBeforeDamageChanged(Entity<BerserkImplantActiveComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!_mobState.IsAlive(ent.Owner))
            return;

        if (!_mobThreshold.TryGetThresholdForState(ent.Owner, MobState.Critical, out var threshold))
            return;

        var currentDamage = Damageable.GetTotalDamage(ent.Owner);

        if (currentDamage + args.Damage.GetTotal() < threshold)
            return;

        args.Cancelled = true;

        if (ent.Comp.DelayedDamage.GetTotal() < 150)
            ent.Comp.DelayedDamage += args.Damage;
    }

    private void OnStaminaDamageModify(Entity<BerserkImplantActiveComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Value *= ent.Comp.StunModifier;
    }

    private void OnGetMeleeDamage(ref GetMeleeDamageEvent args)
    {
        if (!TryComp<BerserkImplantActiveComponent>(args.User, out var Berserk))
            return;

        args.Damage *= Berserk.SelfDamageModifier;
    }

    private void OnShotAttempted(Entity<BerserkImplantActiveComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();
    }
}
