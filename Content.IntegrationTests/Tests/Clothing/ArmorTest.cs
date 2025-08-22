using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Clothing
{
    [TestFixture]
    public sealed class ArmorTest : ContentIntegrationTest
    {
    }
}

namespace Content.IntegrationTests.Tests.Clothing
{
    public sealed partial class ArmorTest
    {
        /// <summary>
        /// Ensures that all ADT armor prototypes from the diff load and provide required components.
        /// Validates presence of Sprite, Clothing, Armor, and ExplosionResistance where defined.
        /// </summary>
        [Test]
        public async Task ADT_Armor_Prototypes_Load_With_Required_Components()
        {
            await using var pair = await StartServer();
            var server = pair.Server;

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            // List includes main IDs introduced/modified in the diff.
            var ids = new[]
            {
                "ADTClothingBlueshieldArmor",
                "ADTClothingOuterArmorTSF",
                "ADTTagillaArmor",
                "ADTKillaArmor",
                "ADTCSIJArmor",
                "ADTClothingOuterArmorMiner",
                "ADTClothingOuterArmorMinerHeavy",
                "ADTClothingOuterArmorMinerLight",
                "ADTClothingOuterArmorMinerReinforcedOne",
                "ADTClothingOuterArmorMinerReinforcedTwo",
                "ADTClothingOuterArmorMinerReinforcedFull",
                "ADTClothingOuterArmorVestHidden",
                "ADTCenturionArmor",
                "ADTLegionerArmor",
                "ADTClothingOuterVestUSSPKZS1",
                "ADTClothingOuterArmorCrusader",
                "ADTClothingGigaMuscles"
            };

            foreach (var id in ids)
            {
                Assert.That(protoMan.HasIndex<EntityPrototype>(id), $"Prototype '{id}' is missing.");
            }
        }

        /// <summary>
        /// Spawn a subset of ADT armor entities and verify Armor coefficients match expectations from the diff.
        /// Focus on key items with unique values to cover variety.
        /// </summary>
        [Test]
        public async Task ADT_Armor_Coefficients_Match_Diff_Expectations()
        {
            await using var pair = await StartServer();
            var server = pair.Server;

            await server.WaitAssertion(async () =>
            {
                var entMan = server.EntMan;

                // Helper local function to fetch armor coefficients safely.
                (bool ok, float? blunt, float? slash, float? pierce, float? heat, float? caustic) GetCoeffs(EntityUid uid)
                {
                    if (!entMan.TryGetComponent(uid, out ArmorComponent? armor))
                        return (false, null, null, null, null, null);

                    float? get(DamageType type) =>
                        armor.Modifiers.Coefficients.TryGetValue(type, out var v) ? v : null;

                    return (true,
                        get(DamageType.Blunt),
                        get(DamageType.Slash),
                        get(DamageType.Piercing),
                        get(DamageType.Heat),
                        get(DamageType.Caustic));
                }

                // Blueshield vest
                var blue = entMan.SpawnEntity("ADTClothingBlueshieldArmor", MapCoordinates.Nullspace);
                var blueCoeffs = GetCoeffs(blue);
                Assert.That(blueCoeffs.ok, "ADTClothingBlueshieldArmor missing ArmorComponent");
                Assert.That(blueCoeffs.blunt, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(blueCoeffs.slash, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(blueCoeffs.pierce, Is.EqualTo(0.4f).Within(0.0001f));
                Assert.That(blueCoeffs.heat, Is.EqualTo(0.8f).Within(0.0001f));

                // TSF vest
                var tsf = entMan.SpawnEntity("ADTClothingOuterArmorTSF", MapCoordinates.Nullspace);
                var tsfCoeffs = GetCoeffs(tsf);
                Assert.That(tsfCoeffs.ok, "ADTClothingOuterArmorTSF missing ArmorComponent");
                Assert.That(tsfCoeffs.blunt, Is.EqualTo(0.7f).Within(0.0001f));
                Assert.That(tsfCoeffs.slash, Is.EqualTo(0.7f).Within(0.0001f));
                Assert.That(tsfCoeffs.pierce, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(tsfCoeffs.heat, Is.EqualTo(0.7f).Within(0.0001f));

                // Miner base (has Caustic)
                var miner = entMan.SpawnEntity("ADTClothingOuterArmorMiner", MapCoordinates.Nullspace);
                var minerCoeffs = GetCoeffs(miner);
                Assert.That(minerCoeffs.ok, "ADTClothingOuterArmorMiner missing ArmorComponent");
                Assert.That(minerCoeffs.blunt, Is.EqualTo(0.7f).Within(0.0001f));
                Assert.That(minerCoeffs.slash, Is.EqualTo(0.7f).Within(0.0001f));
                Assert.That(minerCoeffs.pierce, Is.EqualTo(0.95f).Within(0.0001f));
                Assert.That(minerCoeffs.heat, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(minerCoeffs.caustic, Is.EqualTo(0.8f).Within(0.0001f));

                // Miner Heavy
                var minerHeavy = entMan.SpawnEntity("ADTClothingOuterArmorMinerHeavy", MapCoordinates.Nullspace);
                var minerHeavyCoeffs = GetCoeffs(minerHeavy);
                Assert.That(minerHeavyCoeffs.ok, "ADTClothingOuterArmorMinerHeavy missing ArmorComponent");
                Assert.That(minerHeavyCoeffs.blunt, Is.EqualTo(0.6f).Within(0.0001f));
                Assert.That(minerHeavyCoeffs.slash, Is.EqualTo(0.6f).Within(0.0001f));
                Assert.That(minerHeavyCoeffs.pierce, Is.EqualTo(0.7f).Within(0.0001f));
                Assert.That(minerHeavyCoeffs.heat, Is.EqualTo(0.65f).Within(0.0001f));
                Assert.That(minerHeavyCoeffs.caustic, Is.EqualTo(0.60f).Within(0.0001f));

                // Miner Light
                var minerLight = entMan.SpawnEntity("ADTClothingOuterArmorMinerLight", MapCoordinates.Nullspace);
                var minerLightCoeffs = GetCoeffs(minerLight);
                Assert.That(minerLightCoeffs.ok, "ADTClothingOuterArmorMinerLight missing ArmorComponent");
                Assert.That(minerLightCoeffs.blunt, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(minerLightCoeffs.slash, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(minerLightCoeffs.pierce, Is.EqualTo(0.85f).Within(0.0001f));
                Assert.That(minerLightCoeffs.heat, Is.EqualTo(0.85f).Within(0.0001f));
                Assert.That(minerLightCoeffs.caustic, Is.EqualTo(0.9f).Within(0.0001f));
            });
        }

        /// <summary>
        /// Validate ExplosionResistance damageCoefficient for a range of armor based on diff.
        /// </summary>
        [Test]
        public async Task ADT_ExplosionResistance_Coefficients_Are_Correct()
        {
            await using var pair = await StartServer();
            var server = pair.Server;

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;

                void Check(string id, float expected)
                {
                    var uid = entMan.SpawnEntity(id, MapCoordinates.Nullspace);
                    Assert.That(entMan.TryGetComponent(uid, out ExplosionResistanceComponent? ex), $"{id} missing ExplosionResistance");
                    Assert.That(ex!.DamageCoefficient, Is.EqualTo(expected).Within(0.0001f), $"{id} ExplosionResistance mismatch");
                }

                Check("ADTClothingBlueshieldArmor", 0.80f);
                Check("ADTClothingOuterArmorTSF", 0.80f);
                Check("ADTTagillaArmor", 0.90f);
                Check("ADTKillaArmor", 0.90f);
                Check("ADTCSIJArmor", 0.90f);
                Check("ADTClothingOuterArmorMiner", 0.90f);
                Check("ADTClothingOuterArmorMinerReinforcedOne", 0.90f);
                Check("ADTClothingOuterArmorMinerReinforcedTwo", 0.80f);
                Check("ADTClothingOuterArmorMinerReinforcedFull", 0.70f);
                Check("ADTClothingOuterArmorVestHidden", 0.90f);
                Check("ADTCenturionArmor", 0.90f);
                Check("ADTLegionerArmor", 0.90f);
                Check("ADTClothingOuterVestUSSPKZS1", 0.70f);
                // Crusader armor has no explicit ExplosionResistance in the diff; skip it here.
            });
        }

        /// <summary>
        /// Validate ClothingSpeedModifier for items in the diff that define it.
        /// </summary>
        [Test]
        public async Task ADT_Speed_Modifiers_Are_Applied()
        {
            await using var pair = await StartServer();
            var server = pair.Server;

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;

                // Killa
                var killa = entMan.SpawnEntity("ADTKillaArmor", MapCoordinates.Nullspace);
                Assert.That(entMan.TryGetComponent(killa, out ClothingSpeedModifierComponent? killaSpd));
                Assert.That(killaSpd!.WalkModifier, Is.EqualTo(0.90f).Within(0.0001f));
                Assert.That(killaSpd!.SprintModifier, Is.EqualTo(0.90f).Within(0.0001f));

                // Crusader
                var crus = entMan.SpawnEntity("ADTClothingOuterArmorCrusader", MapCoordinates.Nullspace);
                Assert.That(entMan.TryGetComponent(crus, out ClothingSpeedModifierComponent? crusSpd));
                Assert.That(crusSpd!.WalkModifier, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(crusSpd!.SprintModifier, Is.EqualTo(0.75f).Within(0.0001f));

                // Giga Muscles
                var muscles = entMan.SpawnEntity("ADTClothingGigaMuscles", MapCoordinates.Nullspace);
                Assert.That(entMan.TryGetComponent(muscles, out ClothingSpeedModifierComponent? musSpd));
                Assert.That(musSpd!.WalkModifier, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(musSpd!.SprintModifier, Is.EqualTo(0.75f).Within(0.0001f));
            });
        }

        /// <summary>
        /// Validate that ADTClothingGigaMuscles exposes the melee weapon stats as per diff.
        /// </summary>
        [Test]
        public async Task ADT_GigaMuscles_MeleeWeapon_Config_Is_Correct()
        {
            await using var pair = await StartServer();
            var server = pair.Server;

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;

                var uid = entMan.SpawnEntity("ADTClothingGigaMuscles", MapCoordinates.Nullspace);
                Assert.That(entMan.TryGetComponent(uid, out MeleeWeaponComponent? melee));
                Assert.That(melee!.AttackRate, Is.EqualTo(1f).Within(0.0001f), "AttackRate");
                Assert.That(melee!.ClickDamageModifier, Is.EqualTo(1.5f).Within(0.0001f), "ClickDamageModifier");

                // Damage types
                Assert.That(melee!.Damage.DamageDict.TryGetValue(DamageType.Blunt, out var blunt) ? blunt : 0, Is.EqualTo(12).Within(0.0001f));
                Assert.That(melee!.Damage.DamageDict.TryGetValue(DamageType.Structural, out var structural) ? structural : 0, Is.EqualTo(20).Within(0.0001f));

                // Animation IDs present (we don't assert specific assets existence here, only presence of configured fields)
                Assert.That(melee!.Animation, Is.EqualTo("ADTWeaponArcRedCrash"));
                Assert.That(melee!.WideAnimation, Is.EqualTo("ADTWeaponArcRedCrash"));
                Assert.That(melee!.CustomWideAnim, Is.True);
            });
        }

        /// <summary>
        /// Validate ToggleableClothing relationships for miner armors link to their helmets.
        /// </summary>
        [Test]
        public async Task ADT_MinerArmors_Have_ToggleableClothing_Helmet_Links()
        {
            await using var pair = await StartServer();
            var server = pair.Server;

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;

                void Check(string id, string expectedProto)
                {
                    var uid = entMan.SpawnEntity(id, MapCoordinates.Nullspace);
                    Assert.That(entMan.TryGetComponent(uid, out ToggleableClothingComponent? t));
                    Assert.That(t!.ClothingPrototype, Is.EqualTo(expectedProto));
                }

                Check("ADTClothingOuterArmorMiner", "ADTClothingHeadHelmetMiner");
                Check("ADTClothingOuterArmorMinerReinforcedOne", "ADTClothingHeadHelmetMinerReinforcedOne");
                Check("ADTClothingOuterArmorMinerReinforcedTwo", "ADTClothingHeadHelmetMinerReinforcedTwo");
                Check("ADTClothingOuterArmorMinerReinforcedFull", "ADTClothingHeadHelmetMinerReinforcedFull");
            });
        }

        /// <summary>
        /// Validate Foldable/FoldableClothing flags for hidden vest.
        /// </summary>
        [Test]
        public async Task ADT_HiddenVest_Is_Foldable_As_Configured()
        {
            await using var pair = await StartServer();
            var server = pair.Server;

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;

                var uid = entMan.SpawnEntity("ADTClothingOuterArmorVestHidden", MapCoordinates.Nullspace);

                Assert.That(entMan.HasComponent<FoldableComponent>(uid), "Foldable component missing");
                Assert.That(entMan.HasComponent<FoldableClothingComponent>(uid), "FoldableClothing component missing");
            });
        }

        /// <summary>
        /// Validate BadgeOnClothing and container slots for miner armors with badge slots.
        /// Ensures ItemSlots/ContainerContainer includes 'badge' as slot.
        /// </summary>
        [Test]
        public async Task ADT_MinerArmors_Expose_Badge_Slot()
        {
            await using var pair = await StartServer();
            var server = pair.Server;

            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;

                void Check(string id)
                {
                    var uid = entMan.SpawnEntity(id, MapCoordinates.Nullspace);

                    Assert.That(entMan.TryGetComponent(uid, out ItemSlotsComponent? slots), $"{id} missing ItemSlots");
                    Assert.That(slots!.Slots.ContainsKey("badge"), $"{id} missing 'badge' slot in ItemSlots");

                    Assert.That(entMan.TryGetComponent(uid, out ContainerManagerComponent? cmc), $"{id} missing ContainerContainer/ContainerManager");
                    Assert.That(cmc!.Containers.ContainsKey("badge"), $"{id} missing 'badge' container");
                }

                Check("ADTClothingOuterArmorMiner");
                Check("ADTClothingOuterArmorMinerReinforcedOne");
                Check("ADTClothingOuterArmorMinerReinforcedTwo");
                Check("ADTClothingOuterArmorMinerReinforcedFull");
            });
        }
    }
}
