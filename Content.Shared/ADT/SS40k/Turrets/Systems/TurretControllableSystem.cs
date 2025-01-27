using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.ADT.SS40k.Turrets.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.ADT.SS40k.Turrets.Systems;

public sealed class TurretControllableSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TurretControllableComponent, MapInitEvent>(OnStartup);//заливаем акшон для возврата(можно добавить и другие)
        SubscribeLocalEvent<TurretControllableComponent, ComponentShutdown>(OnShutdown);//чистим\возвращаем
        SubscribeLocalEvent<TurretControllableComponent, ControlReturnActionEvent>(OnReturn);//акшон возврата
        SubscribeLocalEvent<TurretControllableComponent, GettingControlledEvent>(OnGettingControlled);//сохраняем
        SubscribeLocalEvent<TurretControllableComponent, MoveInputEvent>(OnUserMoveInput);
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

        if (component.Controller is not null)
        {
            RaiseLocalEvent((EntityUid)component.Controller, new ReturnToBodyTurretEvent(uid));
            component.Controller = null;
        }
    }

    public void OnStartup(EntityUid uid, TurretControllableComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.ControlReturnActEntity, component.ControlReturnAction);
    }

    public void OnShutdown(EntityUid uid, TurretControllableComponent component, ComponentShutdown args)
    {
        Return(uid, component);

        _actionsSystem.RemoveAction(component.ControlReturnActEntity);
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

    private void OnUserMoveInput(Entity<TurretControllableComponent> turret, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (turret.Comp.IsMoveable)
            return;

        Return(args.Entity, turret);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var entityes = EntityQueryEnumerator<TurretControllableComponent>();
        while (entityes.MoveNext(out var uid, out var comp))
        {
            if (comp.User is not null && comp.Controller is not null && (!_mobStateSystem.IsAlive((EntityUid)comp.User) ||
            TryComp<SleepingComponent>((EntityUid)comp.User, out var _) ||
            TryComp<ForcedSleepingComponent>((EntityUid)comp.User, out var _))) Return(uid, comp); //тут мейби можно убрать проверку на ещё один компонент(нужно тестить)
        }
    }

    /*
    TODO: Сделать чтобы ссд индикатор не отображался на игроке, который управляет турелью
    TODO: Сделать зум для турелей(у меня не получилось хд)
    TODO: Дописать код для работы со станционным ИИ + добавить для этого и в целом UI с выбором какой турелью управлять(чтобы не было такого "1 консоль на 1 турель")
    */
}

/*
                                           ████▒
                             ░             ████▓             ░
                          █████   ░░▒▓▓▓███████████▓▓▒▒░    █████
                          ░█████████████████████████████████████
                       ░▒████████████▓▓▒▒░░  ▓███████████████████▓▒
             ░▓█▓   ▒█████████▓▒░            ▓███████████████████████▓░  ░▓█▓░
            ░█████████████▒                  ▓████████████████████████████████░
              ░███████▓░               ░▒▓▓███     ░░▓██████████████████████
             ▒██████▒      ░    ▓██▓▓▓████████          ░▒█████████████▓█████▒
           ▒██████░       ░███▒█████▓▒ ▒██████             ░▒███████████▒░▓████▓
    ▒█▓░ ░██████░           ▒██████████  █████                ░▓█████████▓ ░█████  ░▓█░
   ▒██████████▓          ▒███▓ ████████░ █████                   ▓█████████░ ██████████░
      ░██████░          ██████▒ ███████▓ ▒███▓                    ▒█████████▒ ██████░
       █████░      ░   ░███████▒ ░▒▓████▒░ ░░▓                    ▒██████████░ █████
      █████▒     ░█▒▓█  ██████████▒ ░▒█████▒██                    ████████████  █████
     ▓█████      ░█░  █ ▒███████████▓▓████████                   ▒████████████▓ ▒████░
     █████▒       ░▒░▒█▓██████████████████████         ░█▒░      ▓█████████████  ████▓
▓▓▓▓▓█████       ▒▓ ███▒▒▓█████▓░░   ░ ░▒█████         ▓████     ██████████████  █████▒▓▓▓                  © Korol_Charodey 2024
██████████░    ████▒█████▓████░ ▒ ▒░░▒ ▒ ░▒███        ▓█████░    ██████████████  █████████
     █████▒  ░██▓  ▓███████████ ▓░▒░░▒ ▓ █████     ▒████████     ▒▒▒▓██████████  █████
     ▒█████  ██▓       ░███████ ▓ ▒░▒▒ ▓ ▓███▒ ▓█████████████        █████████▓ ▒████
      █████░ ██░     ░▒▓███████░░ ░░░▒ ░ ▓░░▒█       ░░░░▒▒▒░        █████████  █████
      ░█████░▓██      ▒██████████▓▓░  ▓▓█▒▒▓▒███░██▓▓▒▓▒▒░░         █████████  █████
   ▓█████████ ░██▓░  ░▓███████▒░████▒ █████▒▒██▓ █▓▒░▒▒▓█████▓    ▓█████████░ ▓████████▒
   ░████▓█████░ ▒███████████▒░▒░▓▓███████▓▒▒██           ██████▓███████████  █████▓████
         ░█████▓         ▓   ███▓▒██████▒█████       ▒░▒░▓███████████████▒ ░█████    ░
           ▒█████▒       ▓░░█░▒▓██▓▓▓▓█▓█▓██▓▓      ▓ ▒ ▒██████████████▓░░█████░
             ▒█████▓      ▓░░▓▒▒██░▒ ░▒ ░░░▒░▒ ░▒  ▓   ██████████████▓░▒█████▒
              ████████▒           ▓▓▒░▓░▒░▒▒▒░  ▒  ▒░▒▒████████████▓▒▓██████▓
            ▓████▓▓██████▓░        ░  ░ ░  ░ ▓░ ▒███████████████████████▓█████▒
              ▒▒    ░▓███████▓▒              ▓███████████████████████▒░    ▒░
                        ░▓██████████▓▒░░     ▓███████████████████▒░
                         ░█████████████████████████████████▓█████
                         █████     ░░▒▒▓▓███████▓█▓▓▒▒░░     ████▓
                                           ████▒
 */
