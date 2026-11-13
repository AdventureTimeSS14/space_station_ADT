using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Lathe;
using Content.Shared.Popups;

namespace Content.Shared.ADT.Lathe;

public sealed class ADTLatheAccessSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, BoundUserInterfaceMessageAttempt>(OnMessageAttempt);
    }

    private void OnMessageAttempt(Entity<LatheComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.Cancelled || args.Message is OpenBoundInterfaceMessage or LatheSyncRequestMessage)
            return;

        if (!TryUse(ent, args.Actor))
            args.Cancel();
    }

    public bool HasAccess(EntityUid lathe, EntityUid user)
    {
        if (!TryComp<AccessReaderComponent>(lathe, out var reader))
            return true;

        var items = _accessReader.FindPotentialAccessItems(user);
        var access = _accessReader.FindAccessTags(user, items);
        _accessReader.FindStationRecordKeys(user, out var stationKeys, items);
        return _accessReader.IsAllowed(access, stationKeys, lathe, reader);
    }

    public bool TryUse(EntityUid lathe, EntityUid user)
    {
        if (HasAccess(lathe, user))
            return true;

        _popup.PopupEntity(Loc.GetString("lathe-access-denied"), lathe, user);
        return false;
    }
}
