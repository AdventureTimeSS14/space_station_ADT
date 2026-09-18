using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Kitchen;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
public sealed class ADTResearchTest : GameTest
{
    [Test]
    public async Task DisciplineValidTierPrerequesitesTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var allTechs = protoManager.EnumeratePrototypes<TechnologyPrototype>().ToList();

            Assert.Multiple(() =>
            {
                foreach (var discipline in protoManager.EnumeratePrototypes<TechDisciplinePrototype>())
                {
                    foreach (var tech in allTechs)
                    {
                        if (tech.Discipline != discipline.ID)
                            continue;

                        if (tech.Tier == 1)
                            continue;

                        Assert.That(tech.Tier, Is.GreaterThan(0), $"Technology {tech} has invalid tier {tech.Tier}.");
                        Assert.That(discipline.TierPrerequisites.ContainsKey(tech.Tier),
                            $"Discipline {discipline.ID} does not have a TierPrerequisites definition for tier {tech.Tier}");
                    }
                }
            });
        });
    }

    [Test]
    public async Task AllTechPrintableTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        var latheSys = entMan.System<SharedLatheSystem>();

        TestContext.Out.WriteLine($"AllTechPrintableTest: testing lathe and assembler recipes.");

        var failures = new List<string>();

        await server.WaitAssertion(() =>
        {
            var allEnts = protoManager.EnumeratePrototypes<EntityPrototype>();
            var latheTechs = new HashSet<ProtoId<LatheRecipePrototype>>();
            foreach (var proto in allEnts)
            {
                if (proto.Abstract)
                    continue;

                if (pair.IsTestPrototype(proto))
                    continue;

                if (!proto.TryGetComponent<LatheComponent>(out var lathe, compFact))
                    continue;

                latheSys.AddRecipesFromPacks(latheTechs, lathe.DynamicPacks);

                if (proto.TryGetComponent<EmagLatheRecipesComponent>(out var emag, compFact))
                    latheSys.AddRecipesFromPacks(latheTechs, emag.EmagDynamicPacks);
            }

            var assemblerRecipes = new Dictionary<string, ProtoId<TechnologyPrototype>>();
            foreach (var recipe in protoManager.EnumeratePrototypes<FoodRecipePrototype>())
            {
                if ((recipe.RecipeType & (int)MicrowaveRecipeType.MedicalAssembler) == 0)
                    continue;

                if (recipe.RequiredTechnology is { } tech)
                    assemblerRecipes[recipe.Result] = tech;
            }

            var unlockedTechs = new HashSet<ProtoId<LatheRecipePrototype>>();
            foreach (var tech in protoManager.EnumeratePrototypes<TechnologyPrototype>())
            {
                unlockedTechs.UnionWith(tech.RecipeUnlocks);
                foreach (var recipe in tech.RecipeUnlocks)
                {
                    if (latheTechs.Contains(recipe))
                        continue;

                    if (protoManager.TryIndex(recipe, out LatheRecipePrototype? recipeProto)
                        && recipeProto.Result is { } result
                        && assemblerRecipes.TryGetValue(result, out var assemblerTech)
                        && assemblerTech == tech.ID)
                        continue;

                    failures.Add($"Recipe '{recipe}' from tech '{tech.ID}' cannot be unlocked on any lathes or assemblers.");
                }
            }

            foreach (var recipe in latheTechs)
            {
                if (!unlockedTechs.Contains(recipe))
                    failures.Add($"Recipe '{recipe}' is dynamic on a lathe but cannot be unlocked by research.");
            }
        });

        if (failures.Count != 0)
        {
            TestContext.Out.WriteLine($"AllTechPrintableTest detected {failures.Count} problem(s):");
            foreach (var failure in failures)
                TestContext.Out.WriteLine(failure);
        }
        else
        {
            TestContext.Out.WriteLine("AllTechPrintableTest: no problems detected.");
        }

        Assert.Multiple(() =>
        {
            foreach (var failure in failures)
                Assert.Fail(failure);
        });
    }
}