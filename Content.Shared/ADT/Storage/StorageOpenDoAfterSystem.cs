using Content.Shared.ADT.Storage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared.ADT.Storage;

/// <summary>
/// Turns opening/closing an <see cref="StorageOpenDoAfterComponent"/> storage into a do-after.
/// Hooks the storage open/close attempt events, so it covers both the click interaction and the
/// right-click verb. The do-after breaks on movement, so a bag that is being dragged can't be opened.
/// </summary>
public sealed class StorageOpenDoAfterSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedEntityStorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageOpenDoAfterComponent, StorageOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<StorageOpenDoAfterComponent, StorageCloseAttemptEvent>(OnCloseAttempt);
        SubscribeLocalEvent<StorageOpenDoAfterComponent, StorageOpenDoAfterEvent>(OnDoAfter);
    }

    private void OnOpenAttempt(Entity<StorageOpenDoAfterComponent> ent, ref StorageOpenAttemptEvent args)
    {
        // Silent attempts are used to decide whether to show the verb; don't start a do-after for those.
        if (args.Cancelled || args.Silent)
            return;

        if (TryDelay(ent, args.User, open: true))
            args.Cancelled = true;
    }

    private void OnCloseAttempt(Entity<StorageOpenDoAfterComponent> ent, ref StorageCloseAttemptEvent args)
    {
        // A null user means a programmatic close (e.g. something being inserted); leave those instant.
        if (args.Cancelled || args.User is not { } user)
            return;

        if (TryDelay(ent, user, open: false))
            args.Cancelled = true;
    }

    /// <summary>
    /// Starts (or ignores a duplicate) do-after and returns true when the immediate toggle should be blocked.
    /// </summary>
    private bool TryDelay(Entity<StorageOpenDoAfterComponent> ent, EntityUid user, bool open)
    {
        // A bag configured with Delay 0 just opens/closes instantly, no do-after.
        if (ent.Comp.Delay <= TimeSpan.Zero)
            return false;

        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.Delay, new StorageOpenDoAfterEvent(open), ent, target: ent)
        {
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfter);

        // Block the instant toggle whether or not the do-after started (a duplicate is already running).
        return true;
    }

    private void OnDoAfter(Entity<StorageOpenDoAfterComponent> ent, ref StorageOpenDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        // Perform exactly the requested operation, not a toggle, so a state change during the wait can't
        // invert the action. Both methods are idempotent and skip the attempt event (no re-entrant do-after).
        if (args.Open)
        {
            // Re-check via a silent attempt so foldable/welded/space guards still apply; our own handler
            // ignores silent attempts, so this doesn't start another do-after.
            if (_storage.CanOpen(args.User, ent, silent: true))
                _storage.OpenStorage(ent);
        }
        else
        {
            _storage.CloseStorage(ent);
        }
    }
}
