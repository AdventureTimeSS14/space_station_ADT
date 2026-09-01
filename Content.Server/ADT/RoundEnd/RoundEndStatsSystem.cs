using Content.Server.ADT.Economy;
using Content.Shared.ADT.RoundEnd;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Slippery;
using Content.Shared.Station.Components;
using Robust.Shared.GameObjects;
using System.Linq;

namespace Content.Server.ADT.RoundEnd;

/// <summary>
///     Tracks ss13-style round statistics (slips, ore mined, species census,
///     corpses, beaten clowns, richest escaped) for the round end message.
/// </summary>
public sealed class RoundEndStatsSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly BankCardSystem _bankCard = default!;
    [Dependency] private readonly Content.Server.Shuttles.Systems.EmergencyShuttleSystem _emergencyShuttle = default!;

    private int _totalSlips;
    private int _clownSlips;
    private int _oreMined;
    private int _clownsBeaten;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlipEvent>(OnSlip);
        SubscribeLocalEvent<Content.Shared.ADT.Mining.OreMinedEvent>(OnOreMined);
        SubscribeLocalEvent<DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RoundEndStatsCollectEvent>(OnStatsCollect);
    }

    private void OnSlip(ref SlipEvent ev)
    {
        _totalSlips++;

        if (IsClown(ev.Slipped))
            _clownSlips++;
    }

    private void OnOreMined(ref Content.Shared.ADT.Mining.OreMinedEvent ev)
    {
        _oreMined += ev.Amount;
    }

    private void OnDamageChanged(DamageChangedEvent ev)
    {
        if (!ev.DamageIncreased || ev.DamageDelta is not { } delta || delta.GetTotal() <= 0)
            return;

        if (IsClown(ev.Damageable.Owner))
            _clownsBeaten++;
    }

    private bool IsClown(EntityUid uid)
    {
        if (!TryComp<MindContainerComponent>(uid, out var mindContainer)
            || !_mind.TryGetMind(uid, out var mindId, out _, mindContainer))
            return false;

        return HasJobPrototype(mindId, "JobClown");
    }

    private bool HasJobPrototype(EntityUid mindId, string proto)
    {
        foreach (var role in _roles.MindGetAllRoleInfo(mindId))
        {
            if (!role.Antagonist && role.Prototype == proto)
                return true;
        }

        return false;
    }

    private void OnStatsCollect(ref RoundEndStatsCollectEvent ev)
    {
        ev.Stats["slips-total"] = _totalSlips;
        ev.Stats["slips-clown"] = _clownSlips;
        ev.Stats["ore-mined"] = _oreMined;
        ev.Stats["clowns-beaten"] = _clownsBeaten;
        ev.Stats["corpses-station"] = CountCorpsesOnStation();

        // census of species among crew minds
        var census = new Dictionary<string, int>();
        var allMinds = EntityQueryEnumerator<MindComponent>();
        while (allMinds.MoveNext(out var mindId, out var mind))
        {
            if (mind.CurrentEntity is not { } mob)
                continue;

            if (!TryComp<HumanoidProfileComponent>(mob, out var profile))
                continue;

            string speciesId = profile.Species;
            census[speciesId] = census.GetValueOrDefault(speciesId) + 1;
        }
        ev.SpeciesCensus = census;

        FindRichestEscaped(ev);
    }

    private int CountCorpsesOnStation()
    {
        var corpses = 0;
        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mobState, out var xform))
        {
            if (mobState.CurrentState != MobState.Dead)
                continue;

            if (xform.GridUid is not { } grid || !HasComp<StationMemberComponent>(grid))
                continue;

            corpses++;
        }

        return corpses;
    }

    private void FindRichestEscaped(RoundEndStatsCollectEvent ev)
    {
        foreach (var account in _bankCard.GetAllAccounts())
        {
            if (account.Mind is not { } mindEnt)
                continue;

            var (mindId, mind) = mindEnt;

            EntityUid? mob = mind.CurrentEntity;
            if (mob is null && mind.OriginalOwnedEntity is { } netEnt)
                mob = GetEntity(netEnt);

            if (mob is null || TerminatingOrDeleted(mob.Value))
                continue;

            if (CompOrNull<MobStateComponent>(mob.Value)?.CurrentState == MobState.Dead)
                continue;

            // only count those who actually escaped on the shuttle
            if (!_emergencyShuttle.IsTargetEscaping(mob.Value))
                continue;

            if (account.Balance > ev.RichestEscapedBalance)
            {
                ev.RichestEscapedBalance = account.Balance;
                ev.RichestEscapedName = string.IsNullOrEmpty(account.Name) ? mind.CharacterName : account.Name;
                var roles = _roles.MindGetAllRoleInfo(mindId);
                var jobRole = roles.FirstOrDefault(r => !r.Antagonist);
                ev.RichestEscapedJob = jobRole.Prototype ?? string.Empty;
            }
        }
    }
}
