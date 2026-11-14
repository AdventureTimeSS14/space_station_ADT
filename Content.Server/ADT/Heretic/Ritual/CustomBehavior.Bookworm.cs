using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.Ritual;

public sealed partial class RitualBookwormBehavior : RitualCustomBehavior
{
    public override bool Execute(RitualData args, out string? outstr)
    {
        outstr = null;
        return true;
    }

    public override void Finalize(RitualData args)
    {
        var entMan = args.EntityManager;
        var lookup = entMan.System<EntityLookupSystem>();
        var access = entMan.System<SharedAccessSystem>();
        var aiSys = entMan.System<SharedStationAiSystem>();
        var idSys = entMan.System<SharedIdCardSystem>();
        var popup = entMan.System<SharedPopupSystem>();
        var handsSys = entMan.System<SharedHandsSystem>();
        var invSys = entMan.System<InventorySystem>();
        var readerSys = entMan.System<AccessReaderSystem>();
        var ids = new HashSet<EntityUid>();

        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 1.5f))
        {
            if (ent == args.Performer)
                continue;
            if (idSys.TryFindIdCard(ent, out var card))
                ids.Add(card.Owner);
            else if (entMan.HasComponent<AccessComponent>(ent))
                ids.Add(ent);
        }

        if (idSys.TryFindIdCard(args.Performer, out var held))
            ids.Add(held.Owner);

        foreach (var heldItem in handsSys.EnumerateHeld(args.Performer))
        {
            if (idSys.TryFindIdCard(heldItem, out var hCard))
                ids.Add(hCard.Owner);
        }

        var slots = invSys.GetSlotEnumerator(args.Performer);
        while (slots.NextItem(out var item))
        {
            if (idSys.TryFindIdCard(item, out var sCard))
                ids.Add(sCard.Owner);
        }

        foreach (var id in ids)
        {
            var acc = entMan.EnsureComponent<AccessComponent>(id);
            var tags = new HashSet<ProtoId<AccessLevelPrototype>>(access.TryGetTags(id, acc) ?? new HashSet<ProtoId<AccessLevelPrototype>>());
            tags.Add("Maintenance");
            tags.Add("External");
            tags.Add("Heretic");
            access.TrySetTags(id, tags, acc);
        }

        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 7f))
        {
            if (!entMan.HasComponent<AirlockComponent>(ent) && !entMan.HasComponent<DoorComponent>(ent))
                continue;

            var reader = entMan.EnsureComponent<AccessReaderComponent>(ent);
            readerSys.TrySetAccesses((ent, reader), new List<ProtoId<AccessLevelPrototype>> { "Heretic" });
            readerSys.SetActive((ent, reader), true);

            if (entMan.TryGetComponent<StationAiWhitelistComponent>(ent, out var white))
                aiSys.SetWhitelistEnabled((ent, white), false, true);
        }

        popup.PopupEntity(Loc.GetString("heretic-ritual-bookworm-success", ("count", ids.Count)), args.Platform, args.Performer, PopupType.Medium);
    }
}
