using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._SD.RMC.Synth;

public abstract class SharedSynthSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SynthComponent, AttackAttemptEvent>(OnMeleeAttempted);
        SubscribeLocalEvent<SynthComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<SynthComponent, TryingToSleepEvent>(OnSleepAttempt);
        SubscribeLocalEvent<SynthComponent, InteractUsingEvent>(OnSynthInteractUsing);

        SubscribeLocalEvent<UseOnSynthBlockedComponent, BeforeRangedInteractEvent>(OnSynthBlockedBeforeRangedInteract);
    }

    private void OnMapInit(Entity<SynthComponent> ent, ref MapInitEvent args)
    {
        MakeSynth(ent);
    }

    protected virtual void MakeSynth(Entity<SynthComponent> ent)
    {
        if (ent.Comp.AddComponents != null)
            EntityManager.AddComponents(ent.Owner, ent.Comp.AddComponents);

        if (ent.Comp.RemoveComponents != null)
            EntityManager.RemoveComponents(ent.Owner, ent.Comp.RemoveComponents);

    }

    private void OnMeleeAttempted(Entity<SynthComponent> ent, ref AttackAttemptEvent args)
    {
        if (ent.Owner != args.Uid)
            return;

        if (ent.Comp.CanUseMeleeWeapons)
            return;

        if (args.Weapon == null)
            return;

        args.Cancel();
        DoSynthUnableToUsePopup(ent, args.Weapon.Value.Owner);
    }

    private void OnShotAttempted(Entity<SynthComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.CanUseGuns)
            return;

        args.Cancel();
        DoSynthUnableToUsePopup(ent, args.Used);
    }

    private void OnSleepAttempt(Entity<SynthComponent> ent, ref TryingToSleepEvent args)
    {
        args.Cancelled = true; // Synths dont sleep
    }

    private void OnSynthInteractUsing(Entity<SynthComponent> synth, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // TODO
        // When limb damage is released, make this system re-used for prosthetic limbs. They use the exact same values in CM13.
        // Give synths robot limbs

        var used = args.Used;
        var user = args.User;
        var selfRepair = args.User == synth.Owner;

    }

    private void OnSynthBlockedBeforeRangedInteract(Entity<UseOnSynthBlockedComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach)
            return;

        if (args.Target == null)
            return;

        if (!_whitelist.CheckBoth(args.Target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return; // Whitelist is so you dont get the popup by clicking on a random object

        if (HasComp<SynthComponent>(args.Target) && !ent.Comp.Reversed)
            args.Handled = true;
        else if (!HasComp<SynthComponent>(args.Target) && ent.Comp.Reversed)
            args.Handled = true;

        if (args.Handled)
        {
            var msg = Loc.GetString(ent.Comp.Popup, ("user", args.User), ("used", args.Used), ("target", args.Target));
            _popup.PopupClient(msg, args.User, args.User, PopupType.SmallCaution);
        }
    }

    public bool HasDamage(EntityUid synth, ProtoId<DamageGroupPrototype> group)
    {
        if (!TryComp<DamageableComponent>(synth, out var damageable))
            return false;

        if (damageable.Damage.Empty)
            return false;

        var damage = damageable.Damage.GetDamagePerGroup(_prototypes);
        var groupDmg = damage.GetValueOrDefault(group);

        if (groupDmg <= FixedPoint2.Zero)
            return false;

        return true;
    }

    public void DoSynthUnableToUsePopup(EntityUid synth, EntityUid tool)
    {
        var msg = Loc.GetString("rmc-species-synth-programming-prevents-use", ("user", synth), ("tool", tool));
        _popup.PopupClient(msg, synth, synth, PopupType.SmallCaution);
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCSynthRepairEvent : SimpleDoAfterEvent;
