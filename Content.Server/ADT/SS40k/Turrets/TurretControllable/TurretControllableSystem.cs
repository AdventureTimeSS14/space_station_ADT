using Content.Shared.Actions;
using Content.Shared.ADT.SS40k.Turrets;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.ADT.SS40k.Turrets.TurretControllable;

public sealed class TurretControllableSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TurretControllableComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TurretControllableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TurretControllableComponent, ControlReturnActionEvent>(OnReturn);
        SubscribeLocalEvent<TurretControllableComponent, GettingControlledEvent>(OnGettingControlled);
    }

    public void OnGettingControlled(EntityUid uid, TurretControllableComponent component, GettingControlledEvent args)
    {
        component.User = args.User;
        component.Controller = args.Controller;
    }

    public void OnReturn(EntityUid uid, TurretControllableComponent component, ControlReturnActionEvent args)
    {
        Return(uid, component);
    }

    public void Return(EntityUid uid, TurretControllableComponent component)
    {
        if (TryComp<MindContainerComponent>(uid, out var mind))
        {
            if (mind.HasMind)
                TryReturnToBody(uid, component);
        }
        component.User = null;
        //suda mojno dopisat raise event, but nahuya?
        if (component.Controller is not null)
        {
            RaiseLocalEvent((EntityUid)component.Controller, new ReturnToBodyTurretEvent(uid));
            component.Controller = null;
        }
    }

    public void OnStartup(EntityUid uid, TurretControllableComponent component, ComponentStartup args)
    {
        _actionsSystem.AddAction(uid, ref component.ControlReturnActEntity, component.ControlReturnAction);
    }
    public void OnShutdown(EntityUid uid, TurretControllableComponent component, ComponentShutdown args)
    {
        Return(uid, component);

        _actionsSystem.RemoveAction(component.ControlReturnActEntity);//maybe nahui? hotya pohui
    }

    public bool TryReturnToBody(EntityUid uid, TurretControllableComponent component)
    {
        if (component.User is not null)
        {
            _mindSystem.ControlMob(uid, (EntityUid)component.User);
            return true;
        }
        else return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        //todo: проверка пользователя(тела игрока на состояние) и поднятие ивента в случае отруба(по ивенту возвращаем в тело(точно ли стоит делать ивентом, а не тупо вернуть в тело?))
        var entityes = EntityQueryEnumerator<TurretControllableComponent>();
        while (entityes.MoveNext(out var uid, out var comp))
            if (comp.User is not null && !_mobStateSystem.IsAlive((EntityUid)comp.User)) Return(uid, comp);
    }

    // public bool IsControlling(EntityUid uid, TurretControllableComponent comp)
    // {
    //     if (TryComp<MindContainerComponent>(uid, out var mind))
    //     {
    //         return mind.HasMind;
    //     }
    //     else
    //     {
    //         comp.User = null;
    //         return false;
    //     }
    // }
}

