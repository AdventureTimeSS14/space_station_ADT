using Content.Server.ADT.Shuttles.Components;
using Content.Server.Shuttles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Shuttles.Systems;

public sealed class ADTHandheldShuttleConsoleSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private ShuttleConsoleSystem _console = default!;
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private ViewSubscriberSystem _viewSubscriber = default!;

    private static readonly ProtoId<TagPrototype> CanPilotTag = "CanPilot";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTHandheldShuttleConsoleComponent, ConsoleShuttleEvent>(OnGetConsole, after: [typeof(ShuttleConsoleSystem)]);
        SubscribeLocalEvent<ADTHandheldShuttleConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen, after: [typeof(ShuttleConsoleSystem)]);
        SubscribeLocalEvent<ADTHandheldShuttleConsoleComponent, ComponentShutdown>(OnShutdown);
        Subs.BuiEvents<ADTHandheldShuttleConsoleComponent>(ShuttleConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUIClosed);
        });
    }

    private void OnGetConsole(Entity<ADTHandheldShuttleConsoleComponent> ent, ref ConsoleShuttleEvent args)
    {
        if (args.Console is { } found && found != ent.Owner)
            ent.Comp.LinkedConsole = found;

        if (ent.Comp.LinkedConsole is { } linked && !HasComp<ShuttleConsoleComponent>(linked))
            ent.Comp.LinkedConsole = null;

        args.Console = ent.Comp.LinkedConsole;

        if (TryComp<DroneConsoleComponent>(ent, out var drone))
            drone.Entity = ent.Comp.LinkedConsole;
    }

    private void OnUIOpen(Entity<ADTHandheldShuttleConsoleComponent> ent, AfterActivatableUIOpenEvent args)
    {
        var target = _console.GetDroneConsole(ent);
        if (target == null)
            return;

        if (TryComp<ActorComponent>(args.User, out var actor) && ent.Comp.Viewers.TryAdd(args.User, target.Value))
            _viewSubscriber.AddViewSubscriber(target.Value, actor.PlayerSession);

        TryPilot(ent, args.User);

        if (Transform(target.Value).GridUid is { } grid)
            _console.RefreshShuttleConsoles(grid);
    }

    private void TryPilot(EntityUid console, EntityUid user)
    {
        if (!_tags.HasTag(user, CanPilotTag)
            || !_blocker.CanInteract(user, console)
            || !TryComp<ShuttleConsoleComponent>(console, out var consoleComp))
        {
            return;
        }

        var pilot = EnsureComp<PilotComponent>(user);
        if (pilot.Console is { } current)
        {
            _console.RemovePilot(user, pilot);

            if (current == console)
                return;

            EnsureComp<PilotComponent>(user);
        }

        _console.AddPilot(console, user, consoleComp);
    }

    private void OnUIClosed(Entity<ADTHandheldShuttleConsoleComponent> ent, ref BoundUIClosedEvent args)
    {
        RemoveViewer(ent, args.Actor);
    }

    private void OnShutdown(Entity<ADTHandheldShuttleConsoleComponent> ent, ref ComponentShutdown args)
    {
        foreach (var viewer in new List<EntityUid>(ent.Comp.Viewers.Keys))
        {
            RemoveViewer(ent, viewer);
        }
    }

    private void RemoveViewer(Entity<ADTHandheldShuttleConsoleComponent> ent, EntityUid viewer)
    {
        if (!ent.Comp.Viewers.Remove(viewer, out var target))
            return;

        if (TryComp<ActorComponent>(viewer, out var actor))
            _viewSubscriber.RemoveViewSubscriber(target, actor.PlayerSession);
    }
}
