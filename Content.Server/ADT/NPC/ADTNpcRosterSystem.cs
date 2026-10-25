using Content.Shared.ADT.NPC.Prototypes;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC;

public sealed class ADTNpcRosterSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly HashSet<(string Roster, int Index)> _taken = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _taken.Clear();
    }

    public ADTNpcRosterEntry? Take(ProtoId<ADTNpcRosterPrototype> roster, EntProtoId? prototype)
    {
        if (!_proto.TryIndex(roster, out var proto))
            return null;

        if (prototype != null && TryTake(proto, prototype) is { } byPrototype)
            return byPrototype;

        return TryTake(proto, null);
    }

    private ADTNpcRosterEntry? TryTake(ADTNpcRosterPrototype roster, EntProtoId? prototype)
    {
        for (var i = 0; i < roster.Entries.Count; i++)
        {
            var entry = roster.Entries[i];

            if (prototype == null ? entry.Prototypes.Count > 0 : !entry.Prototypes.Contains(prototype.Value))
                continue;

            if (!_taken.Add((roster.ID, i)))
                continue;

            return entry;
        }

        return null;
    }
}
