using System.Text;
using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Systems;
using Content.Server.RoundEnd;
using Content.Shared.ADT.Shadowling;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Robust.Shared.Player;

namespace Content.Server.ADT.Shadowling;

public sealed class ADTShadowlingRuleSystem : GameRuleSystem<ADTShadowlingRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ADTShadowlingAbilitySystem _abilities = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTShadowlingRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
        SubscribeLocalEvent<ADTShadowlingRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);

        SubscribeLocalEvent<ADTShadowlingThrallAddedEvent>(OnThrallAdded);
        SubscribeLocalEvent<ADTShadowlingAscendedEvent>(OnAscended);
    }

    protected override void Started(EntityUid uid, ADTShadowlingRuleComponent comp, GameRuleComponent rule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, rule, args);

        var players = Math.Max(_player.PlayerCount, 1);
        comp.RequiredThralls = Math.Clamp(players / comp.ThrallDivisor, comp.MinThralls, comp.MaxThralls);
    }

    private void OnAntagSelected(Entity<ADTShadowlingRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeShadowling(args.EntityUid, ent);
    }

    public bool MakeShadowling(EntityUid target, Entity<ADTShadowlingRuleComponent> rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, rule.Comp.MindRole.Id, mind, true);
        rule.Comp.Minds.Add(mindId);
        _mind.TryAddObjective(mindId, mind, rule.Comp.AscendObjective.Id);

        var comp = EnsureComp<ADTShadowlingComponent>(target);
        comp.RequiredThralls = rule.Comp.RequiredThralls;
        Dirty(target, comp);

        _antag.SendBriefing(target, Loc.GetString("shadowling-briefing-intro"), Color.MediumPurple, rule.Comp.BriefingSound);
        _antag.SendBriefing(target, Loc.GetString("shadowling-briefing-objective", ("count", rule.Comp.RequiredThralls)), Color.MediumPurple, null);

        return true;
    }

    private void OnThrallAdded(ADTShadowlingThrallAddedEvent args)
    {
        var query = EntityQueryEnumerator<ADTShadowlingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule) || comp.WarningAnnounced)
                continue;

            var threshold = (int) (comp.RequiredThralls * comp.WarningFraction);
            if (_abilities.CountLivingThralls() < threshold)
                continue;

            comp.WarningAnnounced = true;
            _chat.DispatchGlobalAnnouncement(Loc.GetString(comp.WarningAnnouncement),
                Loc.GetString(comp.WarningAnnouncer),
                colorOverride: Color.MediumPurple);
        }
    }

    private void OnAscended(ref ADTShadowlingAscendedEvent args)
    {
        var query = EntityQueryEnumerator<ADTShadowlingRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule) || comp.Ascended)
                continue;

            comp.Ascended = true;

            foreach (var mindId in comp.Minds)
            {
                if (_mind.TryFindObjective(mindId, comp.AscendObjective.Id, out var objective))
                    _codeCondition.SetCompleted(objective.Value);
            }

            _roundEnd.RequestRoundEnd(comp.ForcedEvacuation,
                checkCooldown: false,
                text: "shadowling-ascension-evacuation",
                name: "shadowling-ascension-announcer",
                cantRecall: true);
        }
    }

    private void OnTextPrepend(Entity<ADTShadowlingRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var sb = new StringBuilder();

        sb.AppendLine(Loc.GetString(ent.Comp.Ascended
            ? "shadowling-roundend-ascended"
            : "shadowling-roundend-failed"));

        sb.AppendLine(Loc.GetString("shadowling-roundend-thralls",
            ("count", _abilities.CountLivingThralls()),
            ("needed", ent.Comp.RequiredThralls)));

        args.Text = sb.ToString();
    }
}
