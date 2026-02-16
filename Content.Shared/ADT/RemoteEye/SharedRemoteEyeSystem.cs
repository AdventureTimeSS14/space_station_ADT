using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.ADT.RemoteEye.Components;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Shared.ADT.RemoteEye.Systems;

public abstract class SharedRemoteEyeSystem : EntitySystem
{
    [Dependency] protected readonly SharedMapSystem Maps = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly RemoteEyeVisionSystem _vision = default!;

    private EntityQuery<BroadphaseComponent> _broadphaseQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();

        _broadphaseQuery = GetEntityQuery<BroadphaseComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<RemoteEyeConsoleComponent, ActivateInWorldEvent>(OnConsoleActivate);
        SubscribeLocalEvent<RemoteEyeComponent, ReturnFromRemoteEyeEvent>(OnReturnAction);
        SubscribeLocalEvent<RemoteEyeOverlayComponent, AccessibleOverrideEvent>(OnEyeAccessible);
        SubscribeLocalEvent<RemoteEyeOverlayComponent, InRangeOverrideEvent>(OnEyeInRange);
        SubscribeLocalEvent<RemoteEyeOverlayComponent, MenuVisibilityEvent>(OnEyeMenu);
    }

    private bool SetupEye(Entity<RemoteEyeConsoleComponent> ent, EntityCoordinates? coords = null)
    {
        if (ent.Comp.RemoteEye != null)
            return false;

        var proto = ent.Comp.RemoteEyePrototype;

        if (coords == null)
            coords = Transform(ent.Owner).Coordinates;

        if (proto != null)
        {
            ent.Comp.RemoteEye = SpawnAtPosition(proto, coords.Value);

            if (TryComp<RemoteEyeComponent>(ent.Comp.RemoteEye, out var eyeComp))
            {
                eyeComp.Console = ent.Owner;
            }

            Dirty(ent);
        }

        return true;
    }

    private void OnConsoleActivate(Entity<RemoteEyeConsoleComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;

        // If already in eye, return
        if (ent.Comp.OriginalEntity == user)
        {
            ReturnFromEye((ent.Owner, ent.Comp), user);
            args.Handled = true;
            return;
        }

        if (ent.Comp.OriginalEntity != null && ent.Comp.OriginalEntity != user)
        {
            _popup.PopupClient(Loc.GetString("remote-eye-console-occupied"), user, user);
            args.Handled = true;
            return;
        }

        SetupEye(ent);
        TransferToEye((ent.Owner, ent.Comp), user);
        args.Handled = true;
    }

    private void OnReturnAction(Entity<RemoteEyeComponent> ent, ref ReturnFromRemoteEyeEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Console == null)
            return;

        if (!TryComp<RemoteEyeConsoleComponent>(ent.Comp.Console, out var console))
            return;

        ReturnFromEye((ent.Comp.Console.Value, console), args.Performer);
        QueueDel(console.RemoteEye);
        console.RemoteEye = null;
        args.Handled = true;
    }

    private void OnEyeAccessible(Entity<RemoteEyeOverlayComponent> ent, ref AccessibleOverrideEvent args)
    {
        args.Handled = true;

        if (!_xforms.InRange(args.User, args.Target, 0f))
        {
            args.Accessible = false;
            return;
        }

        args.Accessible = true;
    }


    private void OnEyeMenu(Entity<RemoteEyeOverlayComponent> ent, ref MenuVisibilityEvent args)
    {
        args.Visibility &= ~MenuVisibility.NoFov;
    }

    private void OnEyeInRange(Entity<RemoteEyeOverlayComponent> ent, ref InRangeOverrideEvent args)
    {
        args.Handled = true;
        var targetXform = Transform(args.Target);

        if (targetXform.GridUid != Transform(args.User).GridUid)
        {
            return;
        }

        if (!_broadphaseQuery.TryComp(targetXform.GridUid, out var broadphase) || !_gridQuery.TryComp(targetXform.GridUid, out var grid))
        {
            return;
        }

        var targetTile = Maps.LocalToTile(targetXform.GridUid.Value, grid, targetXform.Coordinates);

        args.InRange = _vision.IsAccessible((targetXform.GridUid.Value, broadphase, grid), targetTile);
    }

    public virtual void TransferToEye(Entity<RemoteEyeConsoleComponent> console, EntityUid user) { }

    public virtual void ReturnFromEye(Entity<RemoteEyeConsoleComponent> console, EntityUid user) { }
}

public sealed partial class ReturnFromRemoteEyeEvent : InstantActionEvent
{
}
