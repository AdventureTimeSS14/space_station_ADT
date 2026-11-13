using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Atmos.Reactions;
using Content.Shared.ADT.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
public sealed class GasReactionGuideTest : GameTest
{
    [Test]
    public async Task EveryReactionHasGuideTest()
    {
        var server = Pair.Server;
        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var reactions = protoManager.EnumeratePrototypes<GasReactionPrototype>().Select(x => x.ID).ToHashSet();
            var guides = protoManager.EnumeratePrototypes<GasReactionGuidePrototype>().Select(x => x.ID).ToHashSet();

            Assert.Multiple(() =>
            {
                Assert.That(reactions.Except(guides), Is.Empty, "Gas reactions without a gasReactionGuide prototype.");
                Assert.That(guides.Except(reactions), Is.Empty, "gasReactionGuide prototypes without a gas reaction.");
            });
        });
    }
}
