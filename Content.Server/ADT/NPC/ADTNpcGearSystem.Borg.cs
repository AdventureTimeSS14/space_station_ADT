using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC;

public sealed partial class ADTNpcGearSystem
{
    [Dependency] private SharedBorgSystem _borg = default!;

    public bool MatchesPrototype(EntityUid uid, List<EntProtoId> prototypes)
    {
        if (MetaData(uid).EntityPrototype?.ID is not { } id)
            return false;

        foreach (var proto in prototypes)
        {
            if (proto.Id == id)
                return true;
        }

        return false;
    }

    public EntityUid? FindBorgModule(EntityUid owner, List<EntProtoId> items)
    {
        if (!TryComp<BorgChassisComponent>(owner, out var chassis))
            return null;

        foreach (var module in chassis.ModuleContainer.ContainedEntities)
        {
            if (!HasComp<SelectableBorgModuleComponent>(module) ||
                !TryComp<BorgModuleComponent>(module, out var moduleComp) ||
                !moduleComp.Installed ||
                !TryComp<ItemBorgModuleComponent>(module, out var itemModule))
            {
                continue;
            }

            foreach (var hand in itemModule.Hands)
            {
                if (hand.Item is { } proto && items.Contains(proto))
                    return module;
            }
        }

        return null;
    }

    public EntityUid? FindBorgItem(EntityUid owner, List<EntProtoId> items)
    {
        foreach (var hand in _hands.EnumerateHands(owner))
        {
            if (_hands.TryGetHeldItem(owner, hand, out var held) && MatchesPrototype(held.Value, items))
                return held.Value;
        }

        if (!TryComp<BorgChassisComponent>(owner, out var chassis))
            return null;

        foreach (var module in chassis.ModuleContainer.ContainedEntities)
        {
            if (!TryComp<ItemBorgModuleComponent>(module, out var itemModule))
                continue;

            foreach (var stored in itemModule.StoredItems.Values)
            {
                if (!TerminatingOrDeleted(stored) && MatchesPrototype(stored, items))
                    return stored;
            }
        }

        return null;
    }

    public bool IsBorgItemActive(EntityUid owner, List<EntProtoId> items)
    {
        return _hands.GetActiveItem(owner) is { } active && MatchesPrototype(active, items);
    }

    public bool SelectBorgItem(EntityUid owner, List<EntProtoId> items)
    {
        if (IsBorgItemActive(owner, items))
            return true;

        if (FindBorgModule(owner, items) is not { } module)
            return false;

        _borg.SelectModule(owner, module);

        if (IsBorgItemActive(owner, items))
            return true;

        foreach (var hand in _hands.EnumerateHands(owner))
        {
            if (_hands.TryGetHeldItem(owner, hand, out var held) && MatchesPrototype(held.Value, items))
                return _hands.TrySetActiveHand(owner, hand);
        }

        return false;
    }
}
