using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Heretic.Prototypes;
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
        var readerSys = entMan.System<AccessReaderSystem>();
        var aiSys = entMan.System<SharedStationAiSystem>();
        var idSys = entMan.System<SharedIdCardSystem>();
        var ids = new HashSet<EntityUid>();
        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 1.5f))
        {
            if (ent == args.Performer)
                continue;
            if (entMan.HasComponent<AccessComponent>(ent))
                ids.Add(ent);
        }
        if (idSys.TryFindIdCard(args.Performer, out var held))
            ids.Add(held.Owner);
        foreach (var id in ids)
        {
            if (!entMan.TryGetComponent<AccessComponent>(id, out var acc))
                continue;
            var tags = new HashSet<ProtoId<AccessLevelPrototype>>(access.TryGetTags(id, acc) ?? new HashSet<ProtoId<AccessLevelPrototype>>());
            tags.Add("Maintenance");
            tags.Add("External");
            tags.Add("Heretic");
            access.TrySetTags(id, tags, acc);
        }
        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 7f))
        {
            if (!entMan.HasComponent<AirlockComponent>(ent))
                continue;
            if (!entMan.TryGetComponent<Content.Shared.Access.Components.AccessReaderComponent>(ent, out var reader))
                continue;
            readerSys.TrySetAccesses((ent, reader), new List<ProtoId<AccessLevelPrototype>> { "Heretic" });
            if (entMan.TryGetComponent<StationAiWhitelistComponent>(ent, out var white))
                aiSys.SetWhitelistEnabled((ent, white), false, true);
        }
    }
}
