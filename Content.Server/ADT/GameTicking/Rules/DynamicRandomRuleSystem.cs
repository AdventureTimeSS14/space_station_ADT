using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed class DynamicRandomRuleSystem : GameRuleSystem<DynamicRandomRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;

    private string _ruleCompName = default!;

    public override void Initialize()
    {
        base.Initialize();
        _ruleCompName = _compFact.GetComponentName(typeof(GameRuleComponent));
    }

    protected override void Added(EntityUid uid, DynamicRandomRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
        var weights = _configurationManager.GetCVar(CCVars.DynamicRandomWeightPrototype);

        if (!TryPickPreset(weights, out var preset))
        {
            Log.Error($"{ToPrettyString(uid)} failed to pick any preset. Removing rule.");
            Del(uid);
            return;
        }

        Log.Info($"Selected {preset.ID} as the dynamic preset.");
        _adminLogger.Add(LogType.EventStarted, $"Selected {preset.ID} as the dynamic preset.");

        foreach (var rule in preset.Rules)
        {
            EntityUid ruleEnt;

            if (GameTicker.RunLevel <= GameRunLevel.InRound)
                ruleEnt = GameTicker.AddGameRule(rule);
            else
                GameTicker.StartGameRule(rule, out ruleEnt);

            component.AdditionalGameRules.Add(ruleEnt);
        }
    }

    protected override void Ended(EntityUid uid, DynamicRandomRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (var rule in component.AdditionalGameRules)
        {
            GameTicker.EndGameRule(rule);
        }
    }

    private bool TryPickPreset(ProtoId<WeightedRandomPrototype> weights, [NotNullWhen(true)] out GamePresetPrototype? preset)
    {
        var options = _prototypeManager.Index(weights).Weights.ShallowClone();
        var players = GameTicker.ReadyPlayerCount();

        GamePresetPrototype? selectedPreset = null;
        var sum = options.Values.Sum();
        while (options.Count > 0)
        {
            var accumulated = 0f;
            var rand = _random.NextFloat(sum);
            foreach (var (key, weight) in options)
            {
                accumulated += weight;
                if (accumulated < rand)
                    continue;

                if (!_prototypeManager.TryIndex(key, out selectedPreset))
                    Log.Error($"Invalid preset {selectedPreset} in dynamic rule weights: {weights}");

                options.Remove(key);
                sum -= weight;
                break;
            }

            if (CanPick(selectedPreset, players))
            {
                preset = selectedPreset;
                return true;
            }

            if (selectedPreset != null)
                Log.Info($"Excluding {selectedPreset.ID} from dynamic preset selection.");
        }

        preset = null;
        return false;
    }

    public bool CanPickAny()
    {
        var dynamicRandomPresetId = _configurationManager.GetCVar(CCVars.DynamicRandomWeightPrototype);
        return CanPickAny(dynamicRandomPresetId);
    }

    public bool CanPickAny(ProtoId<WeightedRandomPrototype> weightedPresets)
    {
        var ids = _prototypeManager.Index(weightedPresets).Weights.Keys
            .Select(x => new ProtoId<GamePresetPrototype>(x));

        return CanPickAny(ids);
    }

    public bool CanPickAny(IEnumerable<ProtoId<GamePresetPrototype>> protos)
    {
        var players = GameTicker.ReadyPlayerCount();
        foreach (var id in protos)
        {
            if (!_prototypeManager.TryIndex(id, out var selectedPreset))
                Log.Error($"Invalid preset {selectedPreset} in dynamic rule weights: {id}");

            if (CanPick(selectedPreset, players))
                return true;
        }

        return false;
    }

    private bool CanPick([NotNullWhen(true)] GamePresetPrototype? selected, int players)
    {
        if (selected == null)
            return false;

        foreach (var ruleId in selected.Rules)
        {
            if (!_prototypeManager.TryIndex(ruleId, out EntityPrototype? rule)
                || !rule.TryGetComponent(_ruleCompName, out GameRuleComponent? ruleComp))
            {
                Log.Error($"Encountered invalid rule {ruleId} in preset {selected.ID}");
                return false;
            }

            if (ruleComp.MinPlayers > players && ruleComp.CancelPresetOnTooFewPlayers)
                return false;
        }

        return true;
    }
}
