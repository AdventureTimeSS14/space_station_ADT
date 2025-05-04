using Content.Server.Atmos.Rotting;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Electrocution;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Traits.Assorted;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.ADT.Atmos.Miasma;
using Content.Shared.Clumsy; //ADT-Tweak
using Content.Shared.Changeling.Components;
using Robust.Server.Containers;
using System.Linq;
using Content.Server.Resist; //ADT-Medicine

namespace Content.Server.Medical;

/// <summary>
/// This handles interactions and logic relating to <see cref="DefibrillatorComponent"/>
/// </summary>
public sealed class DefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!; //ADT-Tweak
    [Dependency] private readonly ChatSystem _chatManager = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!; //ADT-Tweak
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DefibrillatorComponent, UseInHandEvent>(OnUseInHand); //ADT-Tweak
        SubscribeLocalEvent<DefibrillatorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty); //ADT-Tweak
        SubscribeLocalEvent<DefibrillatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DefibrillatorComponent, DefibrillatorZapDoAfterEvent>(OnDoAfter);
    }

    //ADT-Tweak
    private void OnUseInHand(EntityUid uid, DefibrillatorComponent component, UseInHandEvent args)
    {
        if (args.Handled || !TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (!TryToggle(uid, component, args.User))
            return;

        args.Handled = true;
        _useDelay.TryResetDelay((uid, useDelay));
    }

    private void OnPowerCellSlotEmpty(EntityUid uid, DefibrillatorComponent component, ref PowerCellSlotEmptyEvent args)
    {
        if (!TerminatingOrDeleted(uid))
            TryDisable(uid, component);
    }
    //End ADT-Tweak

    private void OnAfterInteract(EntityUid uid, DefibrillatorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        args.Handled = TryStartZap(uid, target, args.User, component);
    }

    private void OnDoAfter(EntityUid uid, DefibrillatorComponent component, DefibrillatorZapDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        if (!CanZap(uid, target, args.User, component))
            return;

        args.Handled = true;
        Zap(uid, target, args.User, component);
    }

    //ADT-Tweak
    public bool TryToggle(EntityUid uid, DefibrillatorComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.Enabled
            ? TryDisable(uid, component)
            : TryEnable(uid, component, user);
    }

    public bool TryEnable(EntityUid uid, DefibrillatorComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Enabled)
            return false;

        if (!_powerCell.HasActivatableCharge(uid))
            return false;


        component.LowChargeState = false;
        component.Enabled = true;
        _appearance.SetData(uid, ToggleVisuals.Toggled, true);
        _audio.PlayPvs(component.PowerOnSound, uid);
        return true;
    }

    public bool TryDisable(EntityUid uid, DefibrillatorComponent? component = null) //ADT-Tweak
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Enabled) //ADT-Tweak
            return false;

        component.Enabled = false;
        _appearance.SetData(uid, ToggleVisuals.Toggled, false);

        _audio.PlayPvs(component.PowerOffSound, uid);
        return true;
    }
    //End ADT-Tweak

    /// <summary>
    ///     Checks if you can actually defib a target.
    /// </summary>
    /// <param name="uid">Uid of the defib</param>
    /// <param name="target">Uid of the target getting defibbed</param>
    /// <param name="user">Uid of the entity using the defibrillator</param>
    /// <param name="component">Defib component</param>
    /// <param name="targetCanBeAlive">
    ///     If true, the target can be alive. If false, the function will check if the target is alive and will return false if they are.
    /// </param>
    /// <returns>
    ///     Returns true if the target is valid to be defibed, false otherwise.
    /// </returns>
    public bool CanZap(EntityUid uid, EntityUid target, EntityUid? user = null, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Enabled)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("defibrillator-not-on"), uid, user.Value);
            return false;
        }

        if (_timing.CurTime < component.NextZapTime) //ADT-Tweak
            return false;

        if (!TryComp<MobStateComponent>(target, out var mobState))
            return false;

        if (!_powerCell.HasActivatableCharge(uid, user: user))
            return false;

        if (_mobState.IsAlive(target, mobState))    //ADT-Tweak
            return false;

        return true;
    }

    /// <summary>
    ///     Tries to start defibrillating the target. If the target is valid, will start the defib do-after.
    /// </summary>
    /// <param name="uid">Uid of the defib</param>
    /// <param name="target">Uid of the target getting defibbed</param>
    /// <param name="user">Uid of the entity using the defibrillator</param>
    /// <param name="component">Defib component</param>
    /// <returns>
    ///     Returns true if the defibrillation do-after started, otherwise false.
    /// </returns>
    public bool TryStartZap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanZap(uid, target, user, component))
        {
            return false;
        }

        _audio.PlayPvs(component.ChargeSound, uid);

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.DoAfterDuration, new DefibrillatorZapDoAfterEvent(),
            uid, target, uid)
        {
            NeedHand = true,
            BreakOnMove = !component.AllowDoAfterMovement
        });
    }

    /// <summary>
    ///     Tries to defibrillate the target with the given defibrillator.
    /// </summary>
    public void Zap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent? component = null, MobStateComponent? mob = null, MobThresholdsComponent? thresholds = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(target, ref mob, ref thresholds, false))   //ADT-Tweak
            return;

        // ADT-Tweak: clowns zap themselves
        if (HasComp<ClumsyComponent>(user) && user != target)
        {
            Zap(uid, user, user, component);
            return;
        }
        if (!_powerCell.TryUseActivatableCharge(uid, user: user))
            return;
        //End ADT-Tweak

        _audio.PlayPvs(component.ZapSound, uid);
        _electrocution.TryDoElectrocution(target, null, component.ZapDamage, component.WritheDuration, true, ignoreInsulation: true);
        component.NextZapTime = _timing.CurTime + component.ZapDelay;
        _appearance.SetData(uid, DefibrillatorVisuals.Ready, false);

        ICommonSession? session = null;

        var dead = true;
        // ADT Changeling start
        if (TryComp<ChangelingHeadslugContainerComponent>(target, out var slug))
        {
            _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-changeling-slug"),
                InGameICChatType.Speak, true);

            var headslug = slug.Container.ContainedEntities.First();
            _container.EmptyContainer(slug.Container, true);
            _electrocution.TryDoElectrocution(headslug, null, component.ZapDamage, component.WritheDuration, true, ignoreInsulation: true);
            if (TryComp<ChangelingHeadslugComponent>(headslug, out var slugComp))
            {
                slugComp.Accumulator = 0;
                slugComp.Alerted = false;
                slugComp.Container = null;
            }
            EnsureComp<CanEscapeInventoryComponent>(headslug);

            RemComp(target, slug);
            return;
        }
        // ADT Changeling end

        if (_rotting.IsRotten(target))
        {
            _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-rotten"),
                InGameICChatType.Speak, true);
        }
        else if (HasComp<EmbalmedComponent>(target)) //ADT-Medicine
        {
            _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-embalmed"),
                InGameICChatType.Speak, true);
        }
        else if (TryComp<UnrevivableComponent>(target, out var unrevivable))
        {
            _chatManager.TrySendInGameICMessage(uid, Loc.GetString(unrevivable.ReasonMessage),
                InGameICChatType.Speak, true);
        }
        else
        {
            if (_mobState.IsDead(target, mob))
                _damageable.TryChangeDamage(target, component.ZapHeal, true, origin: uid);

            if (_mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var threshold) &&
                TryComp<DamageableComponent>(target, out var damageableComponent) &&
                damageableComponent.TotalDamage < threshold)
            {
                _mobState.ChangeMobState(target, MobState.Critical, mob, uid);
                dead = false;
            }

            if (_mind.TryGetMind(target, out _, out var mind) &&
                mind.Session is { } playerSession)
            {
                session = playerSession;
                // notify them they're being revived.
                if (mind.CurrentEntity != target)
                {
                    _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind), session);
                }
            }
            else
            {
                _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-no-mind"),
                    InGameICChatType.Speak, true);
            }
        }

        var sound = dead || session == null
            ? component.FailureSound
            : component.SuccessSound;
        _audio.PlayPvs(sound, uid);

        // TODO clean up this clown show above
        var ev = new TargetDefibrillatedEvent(user, (uid, component));
        RaiseLocalEvent(target, ref ev);
    }

    //ADT-Tweak: Ready-Toggle sound FX
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DefibrillatorComponent>();
        while (query.MoveNext(out var uid, out var defib))
        {
            if (defib.NextZapTime == null || _timing.CurTime < defib.NextZapTime)
                continue;

            if (_powerCell.HasActivatableCharge(uid) && defib.LowChargeState != false)
            {
                defib.LowChargeState = false;
            }

            if (!_powerCell.HasActivatableCharge(uid) && defib.LowChargeState != true)
            {

                defib.Enabled = false;
                _appearance.SetData(uid, ToggleVisuals.Toggled, false);
                _audio.PlayPvs(defib.LowChargeSound, uid);
                defib.LowChargeState = true;
                continue;
            }

            if (defib.LowChargeState != true)
            {
                _audio.PlayPvs(defib.ReadySound, uid);
            }
            _appearance.SetData(uid, DefibrillatorVisuals.Ready, true);
            defib.NextZapTime = null;

        }
    }
    //End ADT-Tweak
}
