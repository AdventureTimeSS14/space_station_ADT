<<<<<<< HEAD
=======
using Content.IntegrationTests.Fixtures;
>>>>>>> upstreamwiz/master
using Content.Shared.Kitchen;

namespace Content.IntegrationTests.Tests.WizdenContentFreeze;

/// <summary>
/// These tests are limited to adding a specific type of content, essentially freezing it. If you are a fork developer, you may want to disable these tests.
/// </summary>
<<<<<<< HEAD
public sealed class WizdenContentFreeze
=======
public sealed class WizdenContentFreeze : GameTest
>>>>>>> upstreamwiz/master
{
    /// <summary>
    /// This freeze prohibits the addition of new microwave recipes.
    /// The maintainers decided that the mechanics of cooking food in the microwave should be removed,
    /// and all recipes should be ported to other cooking methods.
    /// All added recipes essentially increase the technical debt of future cooking refactoring.
    ///
    /// https://github.com/space-wizards/space-station-14/issues/8524
    /// </summary>
    [Test]
    public async Task MicrowaveRecipesFreezeTest()
    {
<<<<<<< HEAD
        await using var pair = await PoolManager.GetServerClient();
=======
        var pair = Pair;
>>>>>>> upstreamwiz/master
        var server = pair.Server;

        var protoMan = server.ProtoMan;

        var recipesCount = protoMan.Count<FoodRecipePrototype>();
<<<<<<< HEAD
        var recipesLimit = 332; // ADT: Updated limit from 220 (Corvax пельмени <3 //218)
=======
        var recipesLimit = 218;
>>>>>>> upstreamwiz/master

        if (recipesCount > recipesLimit)
        {
            Assert.Fail($"PLEASE STOP ADDING NEW MICROWAVE RECIPES. MICROWAVE RECIPES ARE FROZEN AND NEED TO BE REPLACED WITH PROPER COOKING MECHANICS! See https://github.com/space-wizards/space-station-14/issues/8524. Keep it under {recipesLimit}. Current count: {recipesCount}");
        }

        if (recipesCount < recipesLimit)
        {
            Assert.Fail($"Oh, you deleted the microwave recipes? YOU ARE SO COOL! Please lower the number of recipes in MicrowaveRecipesFreezeTest from {recipesLimit} to {recipesCount} so that future contributors cannot add new recipes back.");
        }
<<<<<<< HEAD

        await pair.CleanReturnAsync();
=======
>>>>>>> upstreamwiz/master
    }
}
