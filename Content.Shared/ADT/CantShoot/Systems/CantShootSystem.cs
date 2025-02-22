using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared.ADT.CantShoot;

public sealed class CantShootSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CantShootComponent, ShotAttemptedEvent>(OnShootAttempt);
    }

    private void OnShootAttempt(EntityUid uid, CantShootComponent component, ref ShotAttemptedEvent args)
    {
        if (_whitelist.IsWhitelistPass(component.Whitelist, args.Used))
            return;
        if (component.Popup != null)
            _popup.PopupCursor(Loc.GetString(component.Popup, ("used", args.Used)), args.User);
        args.Cancel();
    }
}
