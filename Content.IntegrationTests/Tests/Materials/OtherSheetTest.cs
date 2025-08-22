using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Tag;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.Materials
{
    /// <summary>
    /// Integration tests focused on "other" sheet prototypes changed in the PR diff.
    /// Validates that entities exist, required components are present, stacks and solutions are correct,
    /// and special tags (e.g., ADTUraniumSheet, NoPaint) are applied appropriately.
    /// </summary>
    [TestFixture]
    public sealed class OtherSheetTest
    {
        private static string[] PrototypeIdsUnderTest => new[]
        {
            "SheetPaper",
            "SheetPaper1",
            "SheetPlasma",
            "SheetPlasma10",
            "SheetPlasma1",
            "SheetPlasmaLingering0",
            "SheetPlastic",
            "SheetPlastic10",
            "SheetPlastic1",
            "SheetUranium",
            "SheetUranium1",
            "MaterialSheetMeat",
            "MaterialSheetMeat1",
        };

        [Test]
        public async Task Prototypes_Exist_And_AreParseable()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var protoMan = server.ResolveDependency<IPrototypeManager>();

            foreach (var id in PrototypeIdsUnderTest)
            {
                Assert.That(protoMan.HasIndex<EntityPrototype>(id), $"Prototype '{id}' should exist.");
                var proto = protoMan.Index<EntityPrototype>(id);

                // Basic sanity: each has at least Sprite + Item or Stack (per diff)
                var hasSprite = proto.Components.ContainsKey("Sprite");
                var hasItem = proto.Components.ContainsKey("Item");
                var hasStack = proto.Components.ContainsKey("Stack");
                Assert.IsTrue(hasSprite, $"{id} should define Sprite component");
                Assert.IsTrue(hasItem || hasStack, $"{id} should define Item and/or Stack component");
            }

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task PaperSheet_Solution_And_Stack_Valid()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();
            var protoMan = server.ResolveDependency<IPrototypeManager>();

            // Paper (full stack)
            var paper = await server.WaitPost(() => entManager.SpawnEntity("SheetPaper", MapCoordinates.Nullspace));
            Assert.That(entManager.HasComponent<StackComponent>(paper), "SheetPaper should have StackComponent");
            // Paper solution: Cellulose 3 (from diff)
            Assert.That(entManager.TryGetComponent(paper, out SolutionContainerManagerComponent? scm), "SheetPaper should have SolutionContainerManagerComponent");
            Assert.IsTrue(scm.Solutions.ContainsKey("paper"), "SheetPaper should expose 'paper' solution");
            var solPaper = scm.Solutions["paper"];
            var cellulose = solPaper.Contents.FirstOrDefault(r => r.Reagent.ID == "Cellulose");
            Assert.IsNotNull(cellulose, "Paper solution should contain Cellulose reagent");
            Assert.AreEqual(3, cellulose!.Quantity.Int(), "Paper solution Cellulose quantity should be 3");

            // Single sheet variant: count = 1
            var paper1 = await server.WaitPost(() => entManager.SpawnEntity("SheetPaper1", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(paper1, out StackComponent? stackSingle), "SheetPaper1 should have StackComponent");
            Assert.AreEqual(1, stackSingle!.Count, "SheetPaper1 should have count 1");

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task PlasmaSheets_Solutions_Tags_And_Lingering_Valid()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();

            var full = await server.WaitPost(() => entManager.SpawnEntity("SheetPlasma", MapCoordinates.Nullspace));
            Assert.That(entManager.HasComponent<StackComponent>(full), "SheetPlasma should have Stack");
            Assert.That(entManager.TryGetComponent(full, out TagComponent? tags), "SheetPlasma should have TagComponent");
            Assert.IsTrue(tags!.Tags.Contains("Sheet"), "SheetPlasma should have 'Sheet' tag");
            Assert.IsTrue(tags!.Tags.Contains("NoPaint"), "SheetPlasma should have 'NoPaint' tag per diff");

            Assert.True(entManager.TryGetComponent(full, out SolutionContainerManagerComponent? scm), "SheetPlasma should have solutions");
            var sol = scm!.Solutions["plasma"];
            var reagent = sol.Contents.FirstOrDefault(r => r.Reagent.ID == "Plasma");
            Assert.IsNotNull(reagent, "Plasma solution should contain Plasma");
            Assert.AreEqual(10, reagent!.Quantity.Int(), "Plasma quantity should be 10");
            Assert.IsFalse(sol.CanReact, "Plasma solution should be non-reactive per diff");

            // Count variants
            var s10 = await server.WaitPost(() => entManager.SpawnEntity("SheetPlasma10", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(s10, out StackComponent? st10));
            Assert.AreEqual(10, st10!.Count, "SheetPlasma10 should have count 10");

            var s1 = await server.WaitPost(() => entManager.SpawnEntity("SheetPlasma1", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(s1, out StackComponent? st1));
            Assert.AreEqual(1, st1!.Count, "SheetPlasma1 should have count 1");

            var linger0 = await server.WaitPost(() => entManager.SpawnEntity("SheetPlasmaLingering0", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(linger0, out StackComponent? stl));
            Assert.AreEqual(0, stl!.Count, "SheetPlasmaLingering0 should have count 0");
            Assert.True(stl!.Lingering, "SheetPlasmaLingering0 should be lingering=true");

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task PlasticSheets_Solutions_And_Tags_Valid()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();

            var full = await server.WaitPost(() => entManager.SpawnEntity("SheetPlastic", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(full, out TagComponent? tags), "SheetPlastic should have TagComponent");
            Assert.IsTrue(tags!.Tags.Contains("Plastic"), "SheetPlastic should have 'Plastic' tag");
            Assert.IsTrue(tags!.Tags.Contains("Sheet"), "SheetPlastic should have 'Sheet' tag");
            Assert.IsTrue(tags!.Tags.Contains("NoPaint"), "SheetPlastic should have 'NoPaint' tag per diff");

            Assert.True(entManager.TryGetComponent(full, out SolutionContainerManagerComponent? scm), "SheetPlastic should have solutions");
            var sol = scm!.Solutions["plastic"];
            // Oil 5, Phosphorus 5, non-reactive
            var oil = sol.Contents.FirstOrDefault(r => r.Reagent.ID == "Oil");
            var phos = sol.Contents.FirstOrDefault(r => r.Reagent.ID == "Phosphorus");
            Assert.IsNotNull(oil, "Plastic solution should contain Oil");
            Assert.IsNotNull(phos, "Plastic solution should contain Phosphorus");
            Assert.AreEqual(5, oil!.Quantity.Int(), "Oil quantity should be 5");
            Assert.AreEqual(5, phos!.Quantity.Int(), "Phosphorus quantity should be 5");
            Assert.IsFalse(sol.CanReact, "Plastic solution should be non-reactive");

            var s10 = await server.WaitPost(() => entManager.SpawnEntity("SheetPlastic10", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(s10, out StackComponent? st10));
            Assert.AreEqual(10, st10!.Count, "SheetPlastic10 should have count 10");

            var s1 = await server.WaitPost(() => entManager.SpawnEntity("SheetPlastic1", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(s1, out StackComponent? st1));
            Assert.AreEqual(1, st1!.Count, "SheetPlastic1 should have count 1");

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task UraniumSheets_Tags_FoodAndReagents_AreValid()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();

            var u = await server.WaitPost(() => entManager.SpawnEntity("SheetUranium", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(u, out TagComponent? tags), "SheetUranium should have TagComponent");
            // ADT tweak: Tag ADTUraniumSheet + Sheet
            Assert.IsTrue(tags!.Tags.Contains("ADTUraniumSheet"), "SheetUranium should have 'ADTUraniumSheet' tag (ADT tweak)");
            Assert.IsTrue(tags!.Tags.Contains("Sheet"), "SheetUranium should have 'Sheet' tag");

            // Food-based grindable solution 'food' with Uranium:8, Radium:2 and CanReact=false
            Assert.True(entManager.TryGetComponent(u, out SolutionContainerManagerComponent? scm), "SheetUranium should have solutions");
            var sol = scm!.Solutions["food"];
            var ur = sol.Contents.FirstOrDefault(r => r.Reagent.ID == "Uranium");
            var ra = sol.Contents.FirstOrDefault(r => r.Reagent.ID == "Radium");
            Assert.IsNotNull(ur, "Uranium sheet food solution should contain Uranium reagent");
            Assert.IsNotNull(ra, "Uranium sheet food solution should contain Radium reagent");
            Assert.AreEqual(8, ur!.Quantity.Int(), "Uranium reagent quantity should be 8");
            Assert.AreEqual(2, ra!.Quantity.Int(), "Radium reagent quantity should be 2");
            Assert.IsFalse(sol.CanReact, "Uranium 'food' solution should be non-reactive");

            var u1 = await server.WaitPost(() => entManager.SpawnEntity("SheetUranium1", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(u1, out StackComponent? st1));
            Assert.AreEqual(1, st1!.Count, "SheetUranium1 should have count 1");

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task MeatSheets_Exist_Tag_NoPaint_And_Solution_Composition_Valid()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();

            var full = await server.WaitPost(() => entManager.SpawnEntity("MaterialSheetMeat", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(full, out TagComponent? tags), "MaterialSheetMeat should have TagComponent");
            Assert.IsTrue(tags!.Tags.Contains("Sheet"), "Meat sheet should have 'Sheet' tag");
            Assert.IsTrue(tags!.Tags.Contains("NoPaint"), "Meat sheet should have 'NoPaint' tag per diff");

            // Solution 'meatsheet' with Protein:7, Fat:3; CanReact=false
            Assert.True(entManager.TryGetComponent(full, out SolutionContainerManagerComponent? scm), "Meat sheet should have solutions");
            var sol = scm!.Solutions["meatsheet"];
            var protein = sol.Contents.FirstOrDefault(r => r.Reagent.ID == "Protein");
            var fat = sol.Contents.FirstOrDefault(r => r.Reagent.ID == "Fat");
            Assert.IsNotNull(protein, "Meat sheet should contain Protein");
            Assert.IsNotNull(fat, "Meat sheet should contain Fat");
            Assert.AreEqual(7, protein!.Quantity.Int(), "Protein quantity should be 7");
            Assert.AreEqual(3, fat!.Quantity.Int(), "Fat quantity should be 3");
            Assert.IsFalse(sol.CanReact, "Meat sheet solution should be non-reactive");

            var single = await server.WaitPost(() => entManager.SpawnEntity("MaterialSheetMeat1", MapCoordinates.Nullspace));
            Assert.True(entManager.TryGetComponent(single, out StackComponent? st1));
            Assert.AreEqual(1, st1!.Count, "MaterialSheetMeat1 should have count 1");

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task AllSheetEntities_HaveExpectedCommonTags_And_AppearanceWhereSpecified()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();

            foreach (var id in PrototypeIdsUnderTest)
            {
                var uid = await server.WaitPost(() => entManager.SpawnEntity(id, MapCoordinates.Nullspace));

                if (id is "SheetPlasma" or "SheetPlastic" or "SheetUranium" or "MaterialSheetMeat")
                {
                    Assert.True(entManager.TryGetComponent(uid, out TagComponent? tags), $"{id} should have TagComponent");
                    Assert.IsTrue(tags!.Tags.Contains("Sheet"), $"{id} should include 'Sheet' tag");
                }

                // Where Appearance is specified in diff (e.g., SheetPaper, SheetPlasma, SheetPlastic, SheetUranium),
                // ensure component exists.
                var requiresAppearance = id is "SheetPaper" or "SheetPlasma" or "SheetPlastic" or "SheetUranium";
                if (requiresAppearance)
                    Assert.IsTrue(entManager.HasComponent<AppearanceComponent>(uid), $"{id} should have AppearanceComponent");
            }

            await pair.CleanReturnAsync();
        }
    }
}

#region GeneratedTests_AddedByPRAssistant
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Content.Shared.Tag;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Materials
{
    [TestFixture]
    public sealed class OtherSheetTest_Generated
    {
        private static string[] PrototypeIdsUnderTest => new[]
        {
            "SheetPaper",
            "SheetPaper1",
            "SheetPlasma",
            "SheetPlasma10",
            "SheetPlasma1",
            "SheetPlasmaLingering0",
            "SheetPlastic",
            "SheetPlastic10",
            "SheetPlastic1",
            "SheetUranium",
            "SheetUranium1",
            "MaterialSheetMeat",
            "MaterialSheetMeat1",
        };

        [Test]
        public async Task PrototypesExist_AndBasicComponentsPresent()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var protoMan = server.ResolveDependency<IPrototypeManager>();

            foreach (var id in PrototypeIdsUnderTest)
            {
                Assert.That(protoMan.HasIndex<EntityPrototype>(id), $"Prototype '{id}' should exist.");
                var proto = protoMan.Index<EntityPrototype>(id);
                Assert.IsTrue(proto.Components.ContainsKey("Sprite"), $"{id} should define Sprite");
                Assert.IsTrue(proto.Components.ContainsKey("Item") || proto.Components.ContainsKey("Stack"), $"{id} should define Item and/or Stack");
            }

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task VariantCounts_AreCorrect_For_SingleAndTen()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();

            var map = MapCoordinates.Nullspace;

            var paper1 = await server.WaitPost(() => entManager.SpawnEntity("SheetPaper1", map));
            Assert.True(entManager.TryGetComponent(paper1, out StackComponent? p1));
            Assert.AreEqual(1, p1!.Count);

            var plasma10 = await server.WaitPost(() => entManager.SpawnEntity("SheetPlasma10", map));
            Assert.True(entManager.TryGetComponent(plasma10, out StackComponent? pl10));
            Assert.AreEqual(10, pl10!.Count);

            var plastic10 = await server.WaitPost(() => entManager.SpawnEntity("SheetPlastic10", map));
            Assert.True(entManager.TryGetComponent(plastic10, out StackComponent? ps10));
            Assert.AreEqual(10, ps10!.Count);

            var plastic1 = await server.WaitPost(() => entManager.SpawnEntity("SheetPlastic1", map));
            Assert.True(entManager.TryGetComponent(plastic1, out StackComponent? ps1));
            Assert.Equal(1, ps1!.Count);

            var ur1 = await server.WaitPost(() => entManager.SpawnEntity("SheetUranium1", map));
            Assert.True(entManager.TryGetComponent(ur1, out StackComponent? u1));
            Assert.AreEqual(1, u1!.Count);

            var meat1 = await server.WaitPost(() => entManager.SpawnEntity("MaterialSheetMeat1", map));
            Assert.True(entManager.TryGetComponent(meat1, out StackComponent? m1));
            Assert.AreEqual(1, m1!.Count);

            var linger0 = await server.WaitPost(() => entManager.SpawnEntity("SheetPlasmaLingering0", map));
            Assert.True(entManager.TryGetComponent(linger0, out StackComponent? l0));
            Assert.AreEqual(0, l0!.Count);
            Assert.True(l0!.Lingering, "Lingering flag should be true for SheetPlasmaLingering0");

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task Tags_And_Reagents_Align_With_Diff()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
            var server = pair.Server;
            var entManager = server.ResolveDependency<IEntityManager>();

            var map = MapCoordinates.Nullspace;

            // Plasma: NoPaint + Sheet, solution plasma: Plasma 10, CanReact=false
            var plasma = await server.WaitPost(() => entManager.SpawnEntity("SheetPlasma", map));
            Assert.True(entManager.TryGetComponent(plasma, out TagComponent? pTags));
            Assert.IsTrue(pTags!.Tags.Contains("Sheet"));
            Assert.IsTrue(pTags!.Tags.Contains("NoPaint"));
            Assert.True(entManager.TryGetComponent(plasma, out SolutionContainerManagerComponent? pScm));
            var pSol = pScm!.Solutions["plasma"];
            Assert.AreEqual(10, pSol.Contents.First(r => r.Reagent.ID == "Plasma").Quantity.Int());
            Assert.IsFalse(pSol.CanReact);

            // Plastic: tags Plastic, Sheet, NoPaint; solution Oil 5, Phosphorus 5
            var plastic = await server.WaitPost(() => entManager.SpawnEntity("SheetPlastic", map));
            Assert.True(entManager.TryGetComponent(plastic, out TagComponent? plTags));
            Assert.IsTrue(plTags!.Tags.Contains("Plastic"));
            Assert.IsTrue(plTags!.Tags.Contains("Sheet"));
            Assert.IsTrue(plTags!.Tags.Contains("NoPaint"));
            Assert.True(entManager.TryGetComponent(plastic, out SolutionContainerManagerComponent? plScm));
            var plSol = plScm!.Solutions["plastic"];
            Assert.AreEqual(5, plSol.Contents.First(r => r.Reagent.ID == "Oil").Quantity.Int());
            Assert.AreEqual(5, plSol.Contents.First(r => r.Reagent.ID == "Phosphorus").Quantity.Int());
            Assert.IsFalse(plSol.CanReact);

            // Uranium: tags ADTUraniumSheet + Sheet; solution 'food' Uranium 8, Radium 2, CanReact=false
            var uranium = await server.WaitPost(() => entManager.SpawnEntity("SheetUranium", map));
            Assert.True(entManager.TryGetComponent(uranium, out TagComponent? uTags));
            Assert.IsTrue(uTags!.Tags.Contains("ADTUraniumSheet"));
            Assert.IsTrue(uTags!.Tags.Contains("Sheet"));
            Assert.True(entManager.TryGetComponent(uranium, out SolutionContainerManagerComponent? uScm));
            var uSol = uScm!.Solutions["food"];
            Assert.AreEqual(8, uSol.Contents.First(r => r.Reagent.ID == "Uranium").Quantity.Int());
            Assert.AreEqual(2, uSol.Contents.First(r => r.Reagent.ID == "Radium").Quantity.Int());
            Assert.IsFalse(uSol.CanReact);

            // Meat sheet: tags Sheet + NoPaint; solution 'meatsheet' Protein 7, Fat 3, CanReact=false
            var meat = await server.WaitPost(() => entManager.SpawnEntity("MaterialSheetMeat", map));
            Assert.True(entManager.TryGetComponent(meat, out TagComponent? mTags));
            Assert.IsTrue(mTags!.Tags.Contains("Sheet"));
            Assert.IsTrue(mTags!.Tags.Contains("NoPaint"));
            Assert.True(entManager.TryGetComponent(meat, out SolutionContainerManagerComponent? mScm));
            var mSol = mScm!.Solutions["meatsheet"];
            Assert.AreEqual(7, mSol.Contents.First(r => r.Reagent.ID == "Protein").Quantity.Int());
            Assert.AreEqual(3, mSol.Contents.First(r => r.Reagent.ID == "Fat").Quantity.Int());
            Assert.IsFalse(mSol.CanReact);

            await pair.CleanReturnAsync();
        }
    }
}
#endregion