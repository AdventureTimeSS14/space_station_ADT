using Content.Shared.ADT.AshWalker;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.AshWalker;

public sealed class ADTHealTouchSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly SoundSpecifier TouchSound = new SoundPathSpecifier("/Audio/Magic/staff_healing.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTHealTouchComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTHealTouchComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTHealTouchComponent, ADTHealTouchActionEvent>(OnHealTouch);
    }

    private void OnMapInit(Entity<ADTHealTouchComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.ActionId);
    }

    private void OnShutdown(Entity<ADTHealTouchComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
    }

    private void OnHealTouch(Entity<ADTHealTouchComponent> ent, ref ADTHealTouchActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (target == ent.Owner && !ent.Comp.CanHealSelf)
        {
            _popup.PopupEntity(Loc.GetString("adt-heal-touch-not-self"), ent.Owner, ent.Owner);
            return;
        }

        var used = new ADTHealTouchUsedEvent(ent.Owner);
        RaiseLocalEvent(target, ref used);

        if (used.Handled)
        {
            _audio.PlayPvs(TouchSound, target);
            args.Handled = true;
            return;
        }

        if (!HasComp<MobStateComponent>(target) || !HasComp<DamageableComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("adt-heal-touch-no-target"), ent.Owner, ent.Owner);
            return;
        }

        _damageable.TryChangeDamage(target, ent.Comp.Healing, true, origin: ent.Owner);
        _audio.PlayPvs(TouchSound, target);
        _popup.PopupEntity(Loc.GetString("adt-heal-touch-success", ("target", target)), ent.Owner, ent.Owner);
        args.Handled = true;
    }
}
