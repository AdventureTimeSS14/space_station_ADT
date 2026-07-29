using Content.Shared.ADT.Mech.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Whitelist;

namespace Content.Shared.ADT.Mech.EntitySystems;

public sealed class MechEquipmentLimitSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public bool CanInsert(EntityUid mech, EntityUid equipment, out LocId? reason)
    {
        reason = null;

        if (!TryComp<MechEquipmentLimitComponent>(mech, out var limits) ||
            !TryComp<MechComponent>(mech, out var mechComp))
        {
            return true;
        }

        foreach (var limit in limits.Limits)
        {
            if (_whitelist.IsWhitelistFail(limit.Whitelist, equipment))
                continue;

            var installed = 0;
            foreach (var ent in mechComp.EquipmentContainer.ContainedEntities)
            {
                if (_whitelist.IsWhitelistPass(limit.Whitelist, ent))
                    installed++;
            }

            if (installed < limit.Max)
                continue;

            reason = limit.Popup;
            return false;
        }

        return true;
    }
}
