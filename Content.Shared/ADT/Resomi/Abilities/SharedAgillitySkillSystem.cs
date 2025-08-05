using Robust.Shared.Timing;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Physics.Events;
using Content.Shared.Climbing.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Actions;
using Robust.Shared.Network;
using Content.Shared.Damage.Components;

namespace Content.Shared.ADT.Resomi.Abilities;

public abstract class SharedAgillitySkillSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;

    protected const int BaseCollisionGroup = (int)CollisionGroup.MobMask;

    public override void Initialize()
    {
        SubscribeLocalEvent<AgillitySkillComponent, MapInitEvent>(OnComponentInit);
        SubscribeLocalEvent<AgillitySkillComponent, StartCollideEvent>(DoJump);
        SubscribeLocalEvent<AgillitySkillComponent, SwitchAgillityActionEvent>(SwitchAgility);
        SubscribeLocalEvent<AgillitySkillComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnComponentInit(Entity<AgillitySkillComponent> ent, ref MapInitEvent args)
    {
        _action.AddAction(ent.Owner, ref ent.Comp.SwitchAgilityActionEntity, ent.Comp.SwitchAgilityAction, ent.Owner);
    }

    private void DoJump(Entity<AgillitySkillComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.Active || !ent.Comp.JumpEnabled
            || args.OurFixture.CollisionMask != BaseCollisionGroup
            || args.OtherFixture.CollisionMask != (int)CollisionGroup.TableMask)
            return;

        if (Timing.IsFirstTimePredicted)
            _stamina.TryTakeStamina(ent.Owner, ent.Comp.StaminaDamageOnJump);
        _climb.ForciblySetClimbing(ent.Owner, args.OtherEntity);
        DoJumpEffect(ent);
    }

    private void SwitchAgility(Entity<AgillitySkillComponent> ent, ref SwitchAgillityActionEvent args)
    {
        if (TryComp<StaminaComponent>(ent, out var stamina) && stamina.StaminaDamage > stamina.CritThreshold * 0.50f)
            return;
        if (args.Handled)
            return;
        if (Timing.IsFirstTimePredicted)
            ent.Comp.Active = !ent.Comp.Active;
        if (_net.IsServer)
            Dirty(ent);
        if (!ent.Comp.Active)
            args.Handled = true;

        ToggleAgility(ent, ent.Comp.Active);
    }

    protected void ToggleAgility(Entity<AgillitySkillComponent> ent, bool activate = false)
    {
        ent.Comp.Active = activate;
        ent.Comp.NextUpdate = Timing.CurTime + TimeSpan.FromSeconds(1.5f);
        _popup.PopupPredicted(Loc.GetString($"agility-activated-{ent.Comp.Active.ToString().ToLower()}"), null, ent.Owner, ent.Owner);
        _action.SetToggled(ent.Comp.SwitchAgilityActionEntity, activate);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnRefreshMovespeed(Entity<AgillitySkillComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Active)
            args.ModifySpeed(1f, ent.Comp.SprintSpeedModifier);
    }

    public virtual void DoJumpEffect(Entity<AgillitySkillComponent> ent)
    {
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AgillitySkillComponent>();
        while (query.MoveNext(out var uid, out var resomiComp))
        {
            if (!TryComp<StaminaComponent>(uid, out var stamina)
                || !resomiComp.Active
                || Timing.CurTime < resomiComp.NextUpdate
                || !Timing.IsFirstTimePredicted)
                continue;

            resomiComp.NextUpdate = Timing.CurTime + TimeSpan.FromSeconds(resomiComp.Delay);

            _stamina.TryTakeStamina(uid, resomiComp.StaminaDamagePassive);
            if (stamina.StaminaDamage > stamina.CritThreshold * 0.50f)
                ToggleAgility((uid, resomiComp));
        }
    }
}
