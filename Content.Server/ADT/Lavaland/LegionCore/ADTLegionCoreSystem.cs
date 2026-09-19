using Content.Server.ADT.Generation;
using Content.Shared.ADT.Lavaland.LegionCore;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using StatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;

namespace Content.Server.ADT.Lavaland.LegionCore;

public sealed class ADTLegionCoreSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRegenerativeCoreComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ADTRegenerativeCoreComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTRegenerativeCoreComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ADTRegenerativeCoreComponent, ADTLegionCoreApplyDoAfterEvent>(OnApplyDoAfter);
        SubscribeLocalEvent<ADTRegenerativeCoreComponent, ADTLegionCoreImplantDoAfterEvent>(OnImplantDoAfter);
    }

    private void OnUseInHand(Entity<ADTRegenerativeCoreComponent> core, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartApply(core, args.User, args.User);
    }

    private void OnAfterInteract(Entity<ADTRegenerativeCoreComponent> core, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryStartApply(core, args.User, target);
    }

    private void OnGetVerbs(Entity<ADTRegenerativeCoreComponent> core, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !core.Comp.CanImplant)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("adt-legion-core-verb-implant"),
            Act = () => TryStartImplant(core, user),
        });
    }

    private bool TryStartApply(Entity<ADTRegenerativeCoreComponent> core, EntityUid user, EntityUid target)
    {
        if (!HasComp<DamageableComponent>(target) || !HasComp<MobStateComponent>(target))
            return false;

        if (_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("adt-legion-core-dead-target"), target, user);
            return true;
        }

        var doAfter = new DoAfterArgs(EntityManager,
            user,
            core.Comp.ApplyDelay,
            new ADTLegionCoreApplyDoAfterEvent(),
            core,
            target: target,
            used: core)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnApplyDoAfter(Entity<ADTRegenerativeCoreComponent> core, ref ADTLegionCoreApplyDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        if (_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("adt-legion-core-dead-target"), target, args.User);
            return;
        }

        var onLavaland = Transform(target).MapUid is { } map && HasComp<ADTLavalandGenerationComponent>(map);
        var power = onLavaland ? 1f : core.Comp.WeakenedMultiplier;

        if (!core.Comp.InstantHeal.Empty)
            _damageable.TryChangeDamage(target, core.Comp.InstantHeal * power, true, false, origin: args.User);

        _bloodstream.TryModifyBleedAmount(target, -100f);
        _statusEffects.TryAddStatusEffectDuration(target, core.Comp.StatusEffect, core.Comp.Duration * power);

        if (onLavaland && core.Comp.RemoveStuns)
            RemoveStuns(target);

        if (!onLavaland)
            _popup.PopupEntity(Loc.GetString("adt-legion-core-weakened"), target, args.User);

        if (target == args.User)
        {
            _popup.PopupEntity(Loc.GetString("adt-legion-core-apply-self"), target, args.User);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("adt-legion-core-apply-other",
                    ("target", Identity.Entity(target, EntityManager))),
                target,
                args.User);
        }

        QueueDel(core);
        args.Handled = true;
    }

    private void RemoveStuns(EntityUid target)
    {
        _statusEffects.TryRemoveStatusEffect(target, SharedStunSystem.StunId);
        RemComp<KnockedDownComponent>(target);
    }

    private void TryStartImplant(Entity<ADTRegenerativeCoreComponent> core, EntityUid user)
    {
        if (HasComp<ADTImplantedLegionCoreComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("adt-legion-core-implant-already"), user, user);
            return;
        }

        if (!HasComp<DamageableComponent>(user) || !HasComp<MobStateComponent>(user))
            return;

        var doAfter = new DoAfterArgs(EntityManager,
            user,
            core.Comp.ImplantDelay,
            new ADTLegionCoreImplantDoAfterEvent(),
            core,
            target: user,
            used: core)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnImplantDoAfter(Entity<ADTRegenerativeCoreComponent> core, ref ADTLegionCoreImplantDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (HasComp<ADTImplantedLegionCoreComponent>(args.User))
            return;

        var implanted = EnsureComp<ADTImplantedLegionCoreComponent>(args.User);
        implanted.CellularMultiplier = core.Comp.ImplantCellularMultiplier;

        _popup.PopupEntity(Loc.GetString("adt-legion-core-implant-done"), args.User, args.User);

        QueueDel(core);
        args.Handled = true;
    }
}
