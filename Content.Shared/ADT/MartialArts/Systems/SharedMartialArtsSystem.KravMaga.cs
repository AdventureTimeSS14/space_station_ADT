using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.MartialArts;

public abstract partial class SharedMartialArtsSystem
{
    private void InitializeKravMaga()
    {
        SubscribeLocalEvent<KravMagaComponent, MapInitEvent>(OnKravMagaMapInit);
        SubscribeLocalEvent<KravMagaComponent, KravMagaActionEvent>(OnKravMagaAction);
        SubscribeLocalEvent<KravMagaComponent, MeleeHitEvent>(OnKravMagaMeleeHitEvent);
        SubscribeLocalEvent<KravMagaComponent, ComponentShutdown>(OnKravMagaShutdown);
    }

    private void OnKravMagaMeleeHitEvent(Entity<KravMagaComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count <= 0)
            return;

        foreach (var hitEntity in args.HitEntities)
        {
            if (!HasComp<MobStateComponent>(hitEntity))
                continue;
            if (!TryComp<Content.Shared.Projectiles.RequireProjectileTargetComponent>(hitEntity, out var isDowned))
                continue;

            DoKravMaga(ent, hitEntity, isDowned);
        }
    }

    private void DoKravMaga(Entity<KravMagaComponent> ent, EntityUid hitEntity, Content.Shared.Projectiles.RequireProjectileTargetComponent requireProjectileTargetComponent)
    {
        if (ent.Comp.SelectedMoveComp == null)
            return;
        var moveComp = ent.Comp.SelectedMoveComp;

        switch (ent.Comp.SelectedMove)
        {
            case KravMagaMoves.LegSweep:
                if (_netManager.IsClient)
                    return;

                if (_standingState.IsDown(hitEntity))
                    break;
                _stun.TryKnockdown(hitEntity, TimeSpan.FromSeconds(4), true);
                break;
            case KravMagaMoves.NeckChop:
                var silenceComp = EnsureComp<KravMagaSilencedComponent>(hitEntity);
                silenceComp.SilencedTime = _timing.CurTime + TimeSpan.FromSeconds(moveComp.EffectTime);
                break;
            case KravMagaMoves.LungPunch:
                _stamina.TakeStaminaDamage(hitEntity, moveComp.StaminaDamage, applyResistances: true);
                var blockedComp = EnsureComp<KravMagaBlockedBreathingComponent>(hitEntity);
                blockedComp.BlockedTime = _timing.CurTime + TimeSpan.FromSeconds(moveComp.EffectTime);
                break;
            case null:
                var damage = ent.Comp.BaseDamage;
                if (requireProjectileTargetComponent.Active)
                    damage *= ent.Comp.DownedDamageModifier;

                DoDamage(ent.Owner, hitEntity, "Blunt", damage, out _);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ent.Comp.SelectedMove = null;
        ent.Comp.SelectedMoveComp = null;
    }

    private void OnKravMagaAction(Entity<KravMagaComponent> ent, ref KravMagaActionEvent args)
    {
        var actionEnt = args.Action.Owner;
        if (!TryComp<KravMagaActionComponent>(actionEnt, out var kravActionComp))
            return;

        args.Handled = true;

        _popupSystem.PopupClient(Loc.GetString("krav-maga-ready", ("action", kravActionComp.Name)), ent, ent);
        ent.Comp.SelectedMove = kravActionComp.Configuration;
        ent.Comp.SelectedMoveComp = kravActionComp;
    }

    private void OnKravMagaMapInit(Entity<KravMagaComponent> ent, ref MapInitEvent args)
    {
        if (HasComp<MartialArtsKnowledgeComponent>(ent) || HasComp<Content.Shared.Changeling.Components.ChangelingComponent>(ent))
            return;

        foreach (var actionId in ent.Comp.BaseKravMagaMoves)
        {
            var actions = _actions.AddAction(ent, actionId);
            if (actions != null)
                ent.Comp.KravMagaMoveEntities.Add(actions.Value);
        }
    }

    private void OnKravMagaShutdown(Entity<KravMagaComponent> ent, ref ComponentShutdown args)
    {
        foreach (var action in ent.Comp.KravMagaMoveEntities)
        {
            _actions.RemoveAction(action);
        }
    }
}
