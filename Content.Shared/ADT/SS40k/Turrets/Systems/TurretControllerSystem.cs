using System.Linq;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.ADT.SS40k.Turrets;
using Content.Shared.ADT.SS40k.Turrets.Components;
using Content.Shared.Mind.Components;

namespace Content.Shared.ADT.SS40k.Turrets.Systems;

public sealed class TurretControllerSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurretControllerComponent, InteractHandEvent>(AfterInteract);//закидываем в турель при использовании
        SubscribeLocalEvent<TurretControllerComponent, ComponentShutdown>(OnShutdown);//чистим
        SubscribeLocalEvent<TurretControllerComponent, LinkAttemptEvent>(OnLinkAttempt);//отменяем соединения
        SubscribeLocalEvent<TurretControllerComponent, NewLinkEvent>(OnNewLink); //заливаем турель в компонент контроллера(потенциал в будущем сделать массив турелей с одной консолью)
        SubscribeLocalEvent<TurretControllerComponent, ReturnToBodyTurretEvent>(OnReturn);//чистим
    }

    public void OnReturn(EntityUid uid, TurretControllerComponent component, ReturnToBodyTurretEvent args)
    {
        component.CurrentUser = null;
        component.CurrentTurret = null;
    }

    public void OnLinkAttempt(EntityUid uid, TurretControllerComponent component, LinkAttemptEvent args)
    {
        if (component.CurrentUser is not null)
            args.Cancel();
    }

    public void AfterInteract(EntityUid uid, TurretControllerComponent component, InteractHandEvent args)
    {
        if (TryComp<GhostComponent>(args.User, out var _))
            return;

        if (TryComp<TurretControllableComponent>(args.User, out var _)) // чтобы обьект умеющий взаимодействовать с консолями не мог контролить сам себя
            return;

        if (!TryComp<DeviceLinkSourceComponent>(uid, out var linkSource))
            return;

        if (component.CurrentUser != null)
        {
            var message = Loc.GetString("machine-already-in-use", ("machine", uid));
            _popupSystem.PopupEntity(message, uid, args.User);
            return;
        }

        TryComp<MindContainerComponent>(args.Target, out var mindForTest);//check
        if (mindForTest is null || !mindForTest.HasMind)
            if (linkSource.LinkedPorts.Count != 0)
            {
                var target = linkSource.LinkedPorts.First().Key;
                component.CurrentUser = args.User;
                component.CurrentTurret = target;
                RaiseLocalEvent(target, new GettingControlledEvent(args.User, uid));
                _mindSystem.ControlMob(args.User, target);
            }
    }

    public void OnShutdown(EntityUid uid, TurretControllerComponent component, ComponentShutdown args)
    {
        if (component.CurrentUser is not null && component.CurrentTurret is not null)
        {
            if (!TryComp<TurretControllableComponent>(component.CurrentTurret, out var turretComp))
                return;

            if (turretComp.User is not null)
                _mindSystem.ControlMob((EntityUid)component.CurrentTurret, (EntityUid)component.CurrentUser);
        }
    }

    public void OnNewLink(EntityUid uid, TurretControllerComponent component, NewLinkEvent args)
    {
        if (TryComp<TurretControllableComponent>(args.Sink, out var _))
            component.CurrentTurret = args.Sink;
    }
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
