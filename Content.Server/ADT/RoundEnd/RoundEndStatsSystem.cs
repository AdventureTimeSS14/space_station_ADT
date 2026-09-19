using System.Linq;
using Content.Server.ADT.Economy;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Shared.ADT.LastWords;
using Content.Shared.ADT.Mining;
using Content.Shared.ADT.RoundEnd;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Slippery;
using Content.Shared.Station.Components;
using Robust.Shared.Player;

namespace Content.Server.ADT.RoundEnd;

public sealed class RoundEndStatsSystem : EntitySystem
{
    [Dependency] private readonly BankCardSystem _bankCard = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationIntegritySystem _integrity = default!;

    private FirstDeathRecord? _firstDeath;

    private int _totalSlips;
    private int _clownSlips;
    private int _oreMined;
    private int _clownsBeaten;
    private int _bitesEaten;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundEndStatsCollectEvent>(OnStatsCollect);

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SlipperyComponent, SlipEvent>(OnSlip);
        SubscribeLocalEvent<OreMinedEvent>(OnOreMined);
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BodyComponent, IngestingEvent>(OnIngesting);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _firstDeath = null;
        _totalSlips = 0;
        _clownSlips = 0;
        _oreMined = 0;
        _clownsBeaten = 0;
        _bitesEaten = 0;
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (_firstDeath != null || ev.NewMobState != MobState.Dead)
            return;

        if (!TryComp<MindContainerComponent>(ev.Target, out var container)
            || !_mind.TryGetMind(ev.Target, out var mindId, out var mind, container))
            return;

        var damage = 0;
        if (TryComp<DamageableComponent>(ev.Target, out var damageable))
            damage = (int) _damageable.GetTotalDamage((ev.Target, damageable));

        _firstDeath = new FirstDeathRecord
        {
            Name = mind.CharacterName ?? Name(ev.Target),
            JobLocId = GetJobLocId(mindId),
            Damage = damage,
            LastWords = CompOrNull<LastWordsComponent>(mindId)?.LastWords ?? string.Empty,
        };
    }

    private void OnSlip(Entity<SlipperyComponent> ent, ref SlipEvent ev)
    {
        _totalSlips++;

        if (IsClown(ev.Slipped))
            _clownSlips++;
    }

    private void OnOreMined(ref OreMinedEvent ev)
    {
        _oreMined += ev.Amount;
    }

    private void OnDamageChanged(Entity<DamageableComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta is not { } delta || delta.GetTotal() < 1)
            return;

        if (IsClown(ent.Owner))
            _clownsBeaten++;
    }

    private void OnIngesting(Entity<BodyComponent> ent, ref IngestingEvent args)
    {
        _bitesEaten++;
    }

    private void OnStatsCollect(ref RoundEndStatsCollectEvent ev)
    {
        CollectSummary(ev);
        CollectFirstDeath(ev);
        CollectEconomy(ev);
        CollectMisc(ev);
        CollectSpeciesCensus(ev);
    }

    private void CollectSummary(RoundEndStatsCollectEvent ev)
    {
        var total = 0;
        var survivors = 0;
        var escapees = 0;
        var shuttleEscapees = 0;

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (_roles.MindHasRole<ObserverRoleComponent>(mindId))
                continue;

            total++;

            if (GetMob(mind) is not { } mob || _mobState.IsDead(mob))
                continue;

            survivors++;

            if (!_emergencyShuttle.IsTargetEscaping(mob))
                continue;

            escapees++;
            shuttleEscapees++;
        }

        ev.Add(RoundEndStatCategory.Summary, "round-end-report-station-integrity")
            .WithArg("value", _integrity.GetIntegrity());

        ev.Add(RoundEndStatCategory.Summary, "round-end-report-population", 1)
            .WithArg("value", total);

        if (_emergencyShuttle.EmergencyShuttleArrived)
        {
            ev.Add(RoundEndStatCategory.Summary, "round-end-report-evacuation-rate", 2)
                .WithRate("value", escapees, total);

            ev.Add(RoundEndStatCategory.Summary, "round-end-report-shuttle-rate", 3)
                .WithRate("value", shuttleEscapees, total);
        }

        ev.Add(RoundEndStatCategory.Summary, "round-end-report-survival-rate", 4)
            .WithRate("value", survivors, total);
    }

    private void CollectFirstDeath(RoundEndStatsCollectEvent ev)
    {
        if (_firstDeath is not { } death)
        {
            ev.Add(RoundEndStatCategory.FirstDeath, "round-end-report-no-deaths");
            return;
        }

        ev.Add(RoundEndStatCategory.FirstDeath, "round-end-report-first-death")
            .WithArg("name", death.Name)
            .WithArg("value", death.Damage)
            .WithLocArg("job", death.JobLocId);

        if (death.LastWords.Length > 0)
        {
            ev.Add(RoundEndStatCategory.FirstDeath, "round-end-report-first-death-last-words", 1)
                .WithArg("lastWords", death.LastWords);
        }
    }

    private void CollectEconomy(RoundEndStatsCollectEvent ev)
    {
        var vault = 0;
        var crew = 0;
        var richest = 0;
        Entity<MindComponent>? richestMind = null;
        var richestName = string.Empty;

        foreach (var account in _bankCard.GetAllAccounts())
        {
            if (account.CommandBudgetAccount)
                continue;

            vault += account.Balance;
            crew++;

            if (account.Balance <= richest)
                continue;

            richest = account.Balance;
            richestMind = account.Mind;
            richestName = account.Name;
        }

        ev.Add(RoundEndStatCategory.Economy, "round-end-report-station-vault")
            .WithArg("value", vault);

        if (crew > 0)
        {
            ev.Add(RoundEndStatCategory.Economy, "round-end-report-average-wealth", 1)
                .WithArg("value", vault / crew);
        }

        if (richestMind is { } mind)
        {
            DescribeCharacter(
                ev.Add(RoundEndStatCategory.Economy, "round-end-report-richest", 2)
                    .WithArg("value", richest),
                mind,
                richestName);
        }
        else
        {
            ev.Add(RoundEndStatCategory.Economy, "round-end-report-nobody-rich", 2);
        }
    }

    private void CollectMisc(RoundEndStatsCollectEvent ev)
    {
        ev.Add(RoundEndStatCategory.Misc, "round-end-report-ore-mined")
            .WithArg("value", _oreMined);

        ev.Add(RoundEndStatCategory.Misc, "round-end-report-food-eaten", 1)
            .WithArg("value", _bitesEaten);

        ev.Add(RoundEndStatCategory.Misc, "round-end-report-slips", 2)
            .WithArg("value", _totalSlips)
            .WithArg("clown", _clownSlips);

        ev.Add(RoundEndStatCategory.Misc, "round-end-report-clowns-beaten", 3)
            .WithArg("value", _clownsBeaten);

        ev.Add(RoundEndStatCategory.Misc, "round-end-report-corpses", 4)
            .WithArg("value", CountCorpses());

        var worstDamage = 0;
        Entity<MindComponent>? worstMind = null;

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (GetMob(mind) is not { } mob || _mobState.IsDead(mob))
                continue;

            if (!TryComp<DamageableComponent>(mob, out var damageable))
                continue;

            var total = (int) _damageable.GetTotalDamage((mob, damageable));
            if (total <= worstDamage)
                continue;

            worstDamage = total;
            worstMind = (mindId, mind);
        }

        if (worstMind is { } worst)
        {
            DescribeCharacter(
                ev.Add(RoundEndStatCategory.Misc, "round-end-report-battered-survivor", 5)
                    .WithArg("value", worstDamage),
                worst);
        }
    }

    private void CollectSpeciesCensus(RoundEndStatsCollectEvent ev)
    {
        var census = new Dictionary<string, int>();

        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out _, out var mind))
        {
            if (mind.CurrentEntity is not { } mob)
                continue;

            if (!TryComp<HumanoidProfileComponent>(mob, out var profile))
                continue;

            var species = profile.Species.Id;
            census[species] = census.GetValueOrDefault(species) + 1;
        }

        ev.SpeciesCensus = census;
    }

    private void DescribeCharacter(RoundEndStatEntry entry, Entity<MindComponent> mind, string? nameOverride = null)
    {
        var name = string.IsNullOrEmpty(nameOverride) ? mind.Comp.CharacterName : nameOverride;
        entry.WithArg("name", name ?? Loc.GetString("round-end-report-unknown-name"));
        entry.WithLocArg("job", GetJobLocId(mind.Owner));

        var userId = mind.Comp.UserId ?? mind.Comp.OriginalOwnerUserId;
        if (userId != null && _playerManager.TryGetPlayerData(userId.Value, out var data))
            entry.WithArg("player", data.ContentData()?.Name ?? data.UserName);
        else
            entry.WithArg("player", Loc.GetString("round-end-report-unknown-name"));
    }

    private string GetJobLocId(EntityUid mindId)
    {
        var job = _roles.MindGetAllRoleInfo(mindId).FirstOrDefault(role => !role.Antagonist);
        return job.Name ?? "game-ticker-unknown-role";
    }

    private EntityUid? GetMob(MindComponent mind)
    {
        var mob = mind.CurrentEntity ?? mind.LastMob;
        if (mob is null && mind.OriginalOwnedEntity is { } netEnt)
            mob = GetEntity(netEnt);

        if (mob is not { } uid || TerminatingOrDeleted(uid) || !HasComp<MobStateComponent>(uid))
            return null;

        return uid;
    }

    private int CountCorpses()
    {
        var corpses = 0;
        var query = EntityQueryEnumerator<MobStateComponent, MindContainerComponent, TransformComponent>();
        while (query.MoveNext(out _, out var mobState, out _, out var xform))
        {
            if (mobState.CurrentState != MobState.Dead)
                continue;

            if (xform.GridUid is not { } grid || !HasComp<StationMemberComponent>(grid))
                continue;

            corpses++;
        }

        return corpses;
    }

    private bool IsClown(EntityUid uid)
    {
        if (!TryComp<MindContainerComponent>(uid, out var mindContainer)
            || !_mind.TryGetMind(uid, out var mindId, out _, mindContainer))
            return false;

        foreach (var role in _roles.MindGetAllRoleInfo(mindId))
        {
            if (!role.Antagonist && role.Prototype == "Clown")
                return true;
        }

        return false;
    }

    private struct FirstDeathRecord
    {
        public string Name;
        public string JobLocId;
        public int Damage;
        public string LastWords;
    }
}
