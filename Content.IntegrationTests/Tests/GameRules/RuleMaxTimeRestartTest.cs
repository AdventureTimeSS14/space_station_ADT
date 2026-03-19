using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.GameRules
{
    [TestFixture]
    [TestOf(typeof(MaxTimeRestartRuleSystem))]
    public sealed class RuleMaxTimeRestartTest
    {
        [Test]
        public async Task RestartTest()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { InLobby = true });
            var server = pair.Server;

            // ADT-tweak start: Don't assert zero components - previous tests may leave components
            var initialGameRuleCount = server.EntMan.Count<GameRuleComponent>();
            var initialActiveGameRuleCount = server.EntMan.Count<ActiveGameRuleComponent>();
            if (initialGameRuleCount != 0)
                Assert.Warn($"Initial GameRuleComponent count is {initialGameRuleCount} instead of 0. Previous test may not have cleaned up.");
            if (initialActiveGameRuleCount != 0)
                Assert.Warn($"Initial ActiveGameRuleComponent count is {initialActiveGameRuleCount} instead of 0. Previous test may not have cleaned up.");
            // ADT-tweak end

            var entityManager = server.ResolveDependency<IEntityManager>();
            var sGameTicker = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GameTicker>();
            var sGameTiming = server.ResolveDependency<IGameTiming>();

            MaxTimeRestartRuleComponent maxTime = null;
            EntityUid maxTimeRuleUid = EntityUid.Invalid; // ADT-tweak
            await server.WaitPost(() =>
            {
                sGameTicker.StartGameRule("MaxTimeRestart", out var ruleEntity);
                maxTimeRuleUid = ruleEntity; // ADT-Tweak
                Assert.That(entityManager.TryGetComponent<MaxTimeRestartRuleComponent>(ruleEntity, out maxTime));
            });

            // ADT-tweak start: Check that our specific rule exists, not total count
            Assert.That(maxTimeRuleUid, Is.Not.EqualTo(EntityUid.Invalid), "MaxTimeRestart rule should be created");
            Assert.That(entityManager.EntityExists(maxTimeRuleUid), "MaxTimeRestart rule entity should exist");
            // ADT-tweak end

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
                maxTime.RoundMaxTime = TimeSpan.FromSeconds(3);
                sGameTicker.StartRound();
            });

            // ADT-tweak start: Verify our rule is still active, not total count
            Assert.That(entityManager.EntityExists(maxTimeRuleUid), "MaxTimeRestart rule should still exist after round start");
            Assert.That(entityManager.HasComponent<ActiveGameRuleComponent>(maxTimeRuleUid), "MaxTimeRestart rule should be active");
            // ADT-tweak end

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
            });

            var ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTime.RoundMaxTime.TotalSeconds * 1.1f);
            await pair.RunTicksSync(ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PostRound));
            });

            ticks = sGameTiming.TickRate * (int) Math.Ceiling(maxTime.RoundEndDelay.TotalSeconds * 1.1f);
            await pair.RunTicksSync(ticks);

            await server.WaitAssertion(() =>
            {
                Assert.That(sGameTicker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            });

            await pair.CleanReturnAsync();
        }
    }
}
