using System.Linq;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Objectives.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

#nullable enable

namespace Content.IntegrationTests.Tests.GameRules;

[TestFixture]
public sealed class TraitorRuleTest
{
    private const string TraitorGameRuleProtoId = "Traitor";
    private const string TraitorAntagRoleName = "Traitor";

    private static readonly ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";
    private static readonly ProtoId<NpcFactionPrototype> NanotrasenFaction = "NanoTrasen";

    [Test]
    public async Task TestTraitorObjectives()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings()
        {
            Dirty = true,
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });

        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var compFact = server.ResolveDependency<IComponentFactory>();
        var ticker = server.System<GameTicker>();
        var mindSys = server.System<MindSystem>();
        var roleSys = server.System<RoleSystem>();
        var factionSys = server.System<NpcFactionSystem>();

        // Look up the minimum player count and max total objective difficulty for the game rule
        var minPlayers = 1;
        var maxDifficulty = 0f;

        await server.WaitAssertion(() =>
        {
            Assert.That(protoMan.TryIndex<EntityPrototype>(TraitorGameRuleProtoId, out var gameRuleEntProto),
                $"Failed to lookup traitor game rule entity prototype with ID \"{TraitorGameRuleProtoId}\"!");

            // Используем GetComponent — если компонента нет, тест упадёт с понятной ошибкой
            var gameRule = gameRuleEntProto.GetComponent<GameRuleComponent>(compFact);
            var randomObjectives = gameRuleEntProto.GetComponent<AntagRandomObjectivesComponent>(compFact);

            minPlayers = gameRule.MinPlayers;
            maxDifficulty = randomObjectives.MaxDifficulty;
        });

        // Остальной тест без изменений
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        Assert.That(client.AttachedEntity, Is.Null);
        Assert.That(ticker.PlayerGameStatuses[client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));

        var dummies = await pair.Server.AddDummySessions(minPlayers);
        await pair.RunTicksSync(5);

        Assert.That(pair.Player?.AttachedEntity, Is.Null);
        Assert.That(dummies.All(x => x.AttachedEntity == null));

        await pair.SetAntagPreference(TraitorAntagRoleName, true);

        // ADT-tweak: Clear any existing game rules and disable preset
        await server.WaitAssertion(() =>
        {
            var existingRules = entMan.AllComponents<GameRuleComponent>().ToArray();
            foreach (var rule in existingRules)
            {
                entMan.DeleteEntity(rule.Uid);
            }
            ticker.SetGamePreset((GamePresetPrototype?)null);
        });

        TraitorRuleComponent? traitorRule = null;

        await server.WaitPost(() =>
        {
            var gameRuleEnt = ticker.AddGameRule(TraitorGameRuleProtoId);
            Assert.That(entMan.TryGetComponent(gameRuleEnt, out traitorRule),
                "Failed to get TraitorRuleComponent after adding the game rule");

            ticker.ToggleReadyAll(true);
            ticker.StartRound();
            ticker.StartGameRule(gameRuleEnt);
        });

        await pair.RunTicksSync(10);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses.Values.All(x => x == PlayerGameStatus.JoinedGame));
        Assert.That(client.EntMan.EntityExists(client.AttachedEntity));

        var player = pair.Player!.AttachedEntity!.Value;
        var dummyEnts = dummies.Select(x => x.AttachedEntity ?? default).ToArray();

        Assert.That(entMan.EntityExists(player));
        Assert.That(dummyEnts.All(entMan.EntityExists));

        var mind = mindSys.GetMind(player)!.Value;

        Assert.That(roleSys.MindIsAntagonist(mind));
        Assert.That(factionSys.IsMember(player, SyndicateFaction), Is.True);
        Assert.That(factionSys.IsMember(player, NanotrasenFaction), Is.False);

        Assert.That(traitorRule!.TotalTraitors, Is.EqualTo(1));
        Assert.That(traitorRule.TraitorMinds[0], Is.EqualTo(mind));

        // Check total objective difficulty
        Assert.That(entMan.TryGetComponent<MindComponent>(mind, out var mindComp));
        var totalDifficulty = mindComp!.Objectives.Sum(o => entMan.GetComponent<ObjectiveComponent>(o).Difficulty);

        Assert.That(totalDifficulty, Is.AtMost(maxDifficulty),
            $"MaxDifficulty exceeded! Objectives: {string.Join(", ", mindComp.Objectives.Select(o => FormatObjective(o, entMan)))}");

        Assert.That(mindComp.Objectives, Is.Not.Empty, "No objectives assigned!");

        // ADT-tweak: Clean up game rules
        await server.WaitAssertion(() =>
        {
            var rules = entMan.AllComponents<GameRuleComponent>().ToArray();
            foreach (var rule in rules)
            {
                entMan.DeleteEntity(rule.Uid);
            }
        });

        await pair.CleanReturnAsync();
    }

    private static string FormatObjective(EntityUid objectiveUid, IEntityManager entMan)
    {
        var meta = entMan.GetComponent<MetaDataComponent>(objectiveUid);
        var objective = entMan.GetComponent<ObjectiveComponent>(objectiveUid);
        return $"{meta.EntityName} ({objective.Difficulty})";
    }
}
