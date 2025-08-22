using Content.Shared.Whitelist;
using Content.Shared.Actions.Events;
using Content.Shared.CombatMode;
using Content.Shared.Item;

namespace Content.Shared.ADT.Combat;

public sealed class CombatModePickupWhitelistSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModePickupWhitelistComponent, PickupAttemptEvent>(OnPickup);
    }

    private void OnCombatToggled(EntityUid uid, CombatModePickupWhitelistComponent comp, ref ToggleCombatActionEvent args)
    {
    }

    private void OnPickup(EntityUid uid, CombatModePickupWhitelistComponent comp, ref PickupAttemptEvent args)
    {
        if (!_combat.IsInCombatMode(uid))
            return;

        if (!_whitelist.CheckBoth(args.Item, blacklist: comp.Blacklist, whitelist: comp.Whitelist))
            args.Cancel();
    }
}
