using Content.Shared.ADT.Storage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared.ADT.Storage;

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
        if (args.Cancelled || args.Silent)
            return;

        if (TryDelay(ent, args.User, open: true))
            args.Cancelled = true;
    }

    private void OnCloseAttempt(Entity<StorageOpenDoAfterComponent> ent, ref StorageCloseAttemptEvent args)
    {
        if (args.Cancelled || args.Silent || args.User is not { } user)
            return;

        if (TryDelay(ent, user, open: false))
            args.Cancelled = true;
    }

    private bool TryDelay(Entity<StorageOpenDoAfterComponent> ent, EntityUid user, bool open)
    {
        if (ent.Comp.Delay <= TimeSpan.Zero)
            return false;

        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.Delay, new StorageOpenDoAfterEvent(open), ent, target: ent)
        {
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfter);

        return true;
    }

    private void OnDoAfter(Entity<StorageOpenDoAfterComponent> ent, ref StorageOpenDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Open)
        {
            if (_storage.CanOpen(args.User, ent, silent: true))
                _storage.OpenStorage(ent);
        }
        else
        {
            if (_storage.CanClose(ent, args.User, silent: true))
                _storage.CloseStorage(ent);
        }
    }
}
