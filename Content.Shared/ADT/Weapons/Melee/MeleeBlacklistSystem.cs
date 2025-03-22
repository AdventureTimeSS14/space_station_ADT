using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Tools.Components;
using Content.Shared.Whitelist;
using Content.Shared.Lock;

namespace Content.Shared.Weapons.Melee.MeleeBlacklist;

public sealed class MeleeBlacklistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MeleeBlacklistComponent, AttemptMeleeEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, MeleeBlacklistComponent comp, ref AttemptMeleeEvent args)
    {
        if (_whitelist.IsWhitelistPassOrNull(comp.Blacklist, args.User))
        {
            args.Cancelled = true;
            return;
        }
        if (_whitelist.IsWhitelistFail(comp.Whitelist, args.User))
        {
            args.Cancelled = true;
            return;
        }
    }
}
