using Content.Server.ADT.Dissociative.Components;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Dissociative;

public sealed partial class DissociativeIdentitySystem
{
    private void InitializeAntag()
    {
        SubscribeLocalEvent<AfterAntagEntitySelectedEvent>(OnAfterAntagSelected);
    }

    private void RollAntag(Entity<DissociativeAlterComponent> alter, ICommonSession session)
    {
        alter.Comp.AntagRolled = true;

        var players = _antag.GetActivePlayerCount();
        if (players <= 0)
            return;

        var rules = new List<Entity<AntagSelectionComponent>>();
        var query = EntityQueryEnumerator<ActiveGameRuleComponent, AntagSelectionComponent>();
        while (query.MoveNext(out var uid, out _, out var selection))
        {
            if (HasComp<TraitorRuleComponent>(uid) || HasComp<ThiefRuleComponent>(uid))
                rules.Add((uid, selection));
        }

        _random.Shuffle(rules);

        foreach (var rule in rules)
        {
            foreach (var selector in rule.Comp.Antags)
            {
                if (!_proto.Resolve(selector.Proto, out var def) ||
                    !_antag.CanBeAntag(session, rule, def))
                    continue;

                var chance = (float) _antag.GetTargetAntagCount(rule, players, def) / players;
                if (!_random.Prob(Math.Clamp(chance, 0f, 1f)))
                    continue;

                if (_antag.TryMakeAntag(rule, def, session))
                    return;
            }
        }
    }

    private void OnAfterAntagSelected(ref AfterAntagEntitySelectedEvent args)
    {
        if (!TryComp<DissociativeAlterComponent>(args.EntityUid, out var alter) ||
            args.Def.StartingGear is not { } gear ||
            TerminatingOrDeleted(alter.Host))
            return;

        _loadout.Equip(alter.Host, new List<ProtoId<StartingGearPrototype>> { gear }, null);
    }
}
