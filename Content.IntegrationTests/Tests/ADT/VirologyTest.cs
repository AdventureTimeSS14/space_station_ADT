using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.ADT.Virology;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
[TestOf(typeof(ADTDiseaseSystem))]
public sealed class VirologyTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly ADTDiseaseSystem _disease = default!;
    [SidedDependency(Side.Server)] private readonly BloodstreamSystem _bloodstream = default!;
    [SidedDependency(Side.Server)] private readonly SharedSolutionContainerSystem _solution = default!;
    [SidedDependency(Side.Server)] private readonly ReactiveSystem _reactive = default!;
    [SidedDependency(Side.Server)] private readonly BodySystem _body = default!;
    [SidedDependency(Side.Server)] private readonly StomachSystem _stomach = default!;

    [Test]
    public async Task AllPrototypesValid()
    {
        await Pair.Server.WaitAssertion(() =>
        {
            var diseases = SProtoMan.EnumeratePrototypes<ADTDiseasePrototype>().Where(p => !p.Abstract).ToList();
            Assert.That(diseases, Is.Not.Empty, "Нет ни одного прототипа болезни.");

            foreach (var proto in diseases)
            {
                var disease = ADTDisease.FromPrototype(proto);
                Assert.That(disease.MaxStages, Is.GreaterThan(0), $"{proto.ID}: нет стадий.");

                if (proto.Curable && proto.Cures.Count > 0)
                {
                    foreach (var cure in proto.Cures)
                    {
                        Assert.That(SProtoMan.HasIndex<Shared.Chemistry.Reagent.ReagentPrototype>(cure),
                            $"{proto.ID}: лекарство {cure} не существует.");
                    }
                }

                foreach (var mutation in proto.PossibleMutations)
                {
                    Assert.That(SProtoMan.HasIndex<ADTDiseasePrototype>(mutation),
                        $"{proto.ID}: мутация {mutation} не существует.");
                }

                foreach (var symptom in proto.Symptoms)
                {
                    Assert.That(SProtoMan.HasIndex<ADTSymptomPrototype>(symptom),
                        $"{proto.ID}: симптом {symptom} не существует.");
                }
            }

            var symptoms = SProtoMan.EnumeratePrototypes<ADTSymptomPrototype>().ToList();
            Assert.That(symptoms, Is.Not.Empty, "Нет ни одного прототипа симптома.");

            foreach (var symptom in symptoms)
            {
                Assert.That(symptom.Level, Is.InRange(1, 6), $"{symptom.ID}: уровень вне диапазона 1-6.");
            }
        });
    }

    [Test]
    public async Task ContractAndCure()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            var mob = SEntMan.SpawnEntity("MobHuman", map.GridCoords);

            var disease = ADTDisease.FromPrototype(SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseMagnitis"));
            var contracted = _disease.Contract(mob, disease);

            Assert.That(contracted, Is.Not.Null, "Заражение не сработало.");
            Assert.That(_disease.HasDisease(mob, "ADTDiseaseMagnitis"), "Болезни нет на мобе.");
            Assert.That(contracted!.Stage, Is.EqualTo(1));

            Assert.That(_disease.Contract(mob, disease), Is.Null, "Болезнь продублировалась.");

            _disease.ForceStage(mob, contracted, 3);
            Assert.That(contracted.Stage, Is.EqualTo(3));

            Assert.That(SEntMan.TryGetComponent<BloodstreamComponent>(mob, out var blood));
            _bloodstream.TryAddToBloodstream((mob, blood), new Solution("Iron", 20));
            Assert.That(_disease.HasCure(mob, contracted), "Лекарство в крови не распознано.");

            var carrier = SEntMan.GetComponent<ADTDiseaseCarrierComponent>(mob);
            _disease.Cure((mob, carrier), contracted);

            Assert.That(_disease.HasDisease(mob, "ADTDiseaseMagnitis"), Is.False, "Болезнь не вылечена.");
            Assert.That(carrier.Resistances, Does.Contain("ADTDiseaseMagnitis"), "Иммунитет не выдан.");
            Assert.That(_disease.Contract(mob, disease), Is.Null, "Иммунитет не защищает от повторного заражения.");

            SEntMan.DeleteEntity(mob);
        });
    }

    [Test]
    public async Task AdvanceProperties()
    {
        await Pair.Server.WaitAssertion(() =>
        {
            var sneeze = _disease.CreateAdvance(new List<ProtoId<ADTSymptomPrototype>> { "Sneeze" });

            Assert.That(sneeze.Spread.HasFlag(ADTDiseaseSpread.Airborne), "Чихание должно давать воздушный путь.");
            Assert.That(sneeze.Visibility, Is.EqualTo(ADTDiseaseVisibility.Visible), "Скрытность не должна скрывать вирус.");
            Assert.That(sneeze.Cures, Has.Count.EqualTo(1), "Лекарство не сгенерировано.");
            Assert.That(sneeze.Permeability, Is.EqualTo(1.25f).Within(0.001f));

            var stealthy = _disease.CreateAdvance(new List<ProtoId<ADTSymptomPrototype>> { "ViralAdaptation" });
            Assert.That(stealthy.Visibility.HasFlag(ADTDiseaseVisibility.HiddenPandemic), "Скрытный вирус должен быть невидим.");

            var first = _disease.CreateAdvance(new List<ProtoId<ADTSymptomPrototype>> { "Cough", "Fever" });
            var second = _disease.CreateAdvance(new List<ProtoId<ADTSymptomPrototype>> { "Fever", "Cough" });
            Assert.That(first.GetId(), Is.EqualTo(second.GetId()), "Id вируса зависит от порядка симптомов.");
            Assert.That(first.IsSame(second), "Одинаковые вирусы считаются разными.");

            var evolving = _disease.CreateAdvance(new List<ProtoId<ADTSymptomPrototype>> { "Cough" });
            _disease.Evolve(evolving, 1, 2);
            Assert.That(evolving.Symptoms, Has.Count.GreaterThanOrEqualTo(2), "Evolve не добавил симптом.");

            _disease.Devolve(evolving);
            Assert.That(evolving.Symptoms, Has.Count.EqualTo(1), "Devolve не убрал симптом.");

            var left = _disease.CreateAdvance(new List<ProtoId<ADTSymptomPrototype>> { "Cough" });
            var right = _disease.CreateAdvance(new List<ProtoId<ADTSymptomPrototype>> { "Fever" });
            _disease.Mix(left, right);
            Assert.That(left.Symptoms, Does.Contain(new ProtoId<ADTSymptomPrototype>("Fever")), "Mix не перенёс симптом.");
        });
    }

    [Test]
    public async Task SolutionMutation()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            var beaker = SEntMan.SpawnEntity("Beaker", map.GridCoords);
            Assert.That(_solution.TryGetSolution(beaker, "beaker", out var solutionEnt, out _),
                "У стакана нет раствора.");

            var virus = _disease.CreateAdvance(new List<ProtoId<ADTSymptomPrototype>> { "Cough" });
            var data = new ADTVirusData { Diseases = { virus } };
            _solution.TryAddReagent(solutionEnt!.Value, new ReagentId("Blood", new List<ReagentData> { data }), 20, out _);

            Assert.That(_disease.ModifySolutionAdvance(solutionEnt.Value, d => _disease.Evolve(d, 1, 2)),
                "Мутация в растворе не сработала.");

            _solution.ResolveSolution(beaker, "beaker", ref solutionEnt, out var solution);
            var updated = ADTDiseaseSystem.FindVirusData(solution!, out _);
            Assert.That(updated, Is.Not.Null, "Данные вируса потерялись.");

            var advance = updated!.Diseases.First(d => d.IsAdvance);
            Assert.That(advance.Symptoms, Has.Count.GreaterThanOrEqualTo(2), "Симптом не добавился.");

            SEntMan.DeleteEntity(beaker);
        });
    }

    [Test]
    public async Task VirusTravelsInBlood()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            var mob = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var disease = ADTDisease.FromPrototype(SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseFlu"));
            Assert.That(_disease.Contract(mob, disease), Is.Not.Null);

            var blood = SEntMan.GetComponent<BloodstreamComponent>(mob);
            Assert.That(_solution.ResolveSolution(mob, blood.BloodSolutionName, ref blood.BloodSolution, out var solution));

            var data = ADTDiseaseSystem.FindVirusData(solution!, out _);
            Assert.That(data, Is.Not.Null, "В крови нет данных о вирусе.");
            Assert.That(data!.Diseases.Any(d => d.Prototype == "ADTDiseaseFlu"), "В крови нет нужного вируса.");

            SEntMan.DeleteEntity(mob);
        });
    }

    [Test]
    public async Task PandemicEntities()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            foreach (var id in new[] { "ADTPandemic", "ADTVirusCultureBottle", "ADTVaccineBottle", "ADTBodyBagBiohazard" })
            {
                Assert.That(SProtoMan.HasIndex<Robust.Shared.Prototypes.EntityPrototype>(id), $"Нет прототипа {id}.");
                var spawned = SEntMan.SpawnEntity(id, map.GridCoords);
                Assert.That(SEntMan.EntityExists(spawned), $"{id} не заспавнился.");
                SEntMan.DeleteEntity(spawned);
            }
        });
    }

    [Test]
    public async Task VaccineCuresAndProtects()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            var mob = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var disease = ADTDisease.FromPrototype(SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseFlu"));
            Assert.That(_disease.Contract(mob, disease), Is.Not.Null);

            var vaccine = new ADTVaccineData();
            vaccine.Ids.Add("ADTDiseaseFlu");

            var solution = new Solution("ADTVaccine", 15, new List<ReagentData> { vaccine });
            _reactive.DoEntityReaction(mob, solution, ReactionMethod.Injection);

            Assert.That(_disease.HasDisease(mob, "ADTDiseaseFlu"), Is.False, "Вакцина не вылечила болезнь.");

            var carrier = SEntMan.GetComponent<ADTDiseaseCarrierComponent>(mob);
            Assert.That(carrier.Resistances, Does.Contain("ADTDiseaseFlu"), "Вакцина не дала иммунитет.");

            SEntMan.DeleteEntity(mob);
        });
    }

    [Test]
    public async Task CommandsHaveCompletions()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            var mob = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var net = SEntMan.GetNetEntity(mob).ToString();

            Assert.That(_disease.Contract(mob, ADTDisease.FromPrototype(
                SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseCold"))), Is.Not.Null);

            var console = IoCManager.Resolve<IConsoleHost>();
            var shell = new IConsoleShell[] { console.LocalShell }[0];

            foreach (var name in new[] { "adtinfect", "adtcure", "adtdiseases", "adtdiseasestage" })
            {
                Assert.That(console.AvailableCommands.TryGetValue(name, out var cmd), $"Нет команды {name}.");

                var first = cmd!.GetCompletion(shell, new[] { "" });
                Assert.That(first.Options, Is.Not.Empty, $"{name}: нет автодополнения для uid.");
            }

            var infect = console.AvailableCommands["adtinfect"];
            var diseases = infect.GetCompletion(shell, new[] { net, "" });
            Assert.That(diseases.Options.Any(o => o.Value == "ADTDiseaseCold"), "adtinfect не предлагает болезни.");
            Assert.That(diseases.Options.Any(o => o.Value == "advance"), "adtinfect не предлагает advance.");

            var symptoms = infect.GetCompletion(shell, new[] { net, "advance", "" });
            Assert.That(symptoms.Options.Any(o => o.Value == "Sneeze"), "adtinfect не предлагает симптомы.");

            var cure = console.AvailableCommands["adtcure"];
            var flags = cure.GetCompletion(shell, new[] { net, "" });
            Assert.That(flags.Options.Any(o => o.Value == "true"), "adtcure не предлагает флаг иммунитета.");

            var stage = console.AvailableCommands["adtdiseasestage"];
            var index = stage.GetCompletion(shell, new[] { net, "" });
            Assert.That(index.Options, Is.Not.Empty, "adtdiseasestage не предлагает индекс болезни.");

            var stages = stage.GetCompletion(shell, new[] { net, "0", "" });
            Assert.That(stages.Options.Any(o => o.Value == "3"), "adtdiseasestage не предлагает стадии.");

            SEntMan.DeleteEntity(mob);
        });
    }

    [Test]
    public async Task RestCureNeedsSleep()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            var mob = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var cold = _disease.Contract(mob, ADTDisease.FromPrototype(
                SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseCold")));

            Assert.That(cold, Is.Not.Null);
            Assert.That(SEntMan.TryGetComponent<BloodstreamComponent>(mob, out var blood));
            _bloodstream.TryAddToBloodstream((mob, blood), new Solution("ADTSpaceacillin", 20));

            var standing = 0;
            for (var i = 0; i < 50; i++)
            {
                if (_disease.HasCure(mob, cold!))
                    standing++;
            }

            Assert.That(standing, Is.LessThan(15), "Стоя простуда должна почти не лечиться.");

            SEntMan.AddComponent<SleepingComponent>(mob);
            Assert.That(_disease.HasCure(mob, cold!), "Во сне с космоциллином простуда должна лечиться всегда.");

            SEntMan.DeleteEntity(mob);
        });
    }

    [Test]
    public async Task SpanishFluNeedsFluAntibodies()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            var mob = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var spanish = _disease.Contract(mob, ADTDisease.FromPrototype(
                SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseFluSpanish")));

            Assert.That(spanish, Is.Not.Null);
            Assert.That(SEntMan.TryGetComponent<BloodstreamComponent>(mob, out var blood));
            _bloodstream.TryAddToBloodstream((mob, blood), new Solution("ADTSpaceacillin", 20));

            var without = 0;
            for (var i = 0; i < 50; i++)
            {
                if (_disease.HasCure(mob, spanish!))
                    without++;
            }

            Assert.That(without, Is.LessThan(10), "Без антител к гриппу космоциллин почти не помогает.");

            _disease.AddResistances(mob, new HashSet<string> { "ADTDiseaseFlu" });
            Assert.That(_disease.HasCure(mob, spanish!), "С антителами к гриппу космоциллин должен лечить.");

            SEntMan.DeleteEntity(mob);
        });
    }

    [Test]
    public async Task PortedSpritesResolve()
    {
        var ids = new[] { "ADTPandemic", "ADTBioBag", "ADTBodyBagBiohazard", "ADTVirusCultureBottle", "ADTVaccineBottle" };

        await Pair.Client.WaitPost(() =>
        {
            foreach (var id in ids)
            {
                var dummy = Pair.Client.EntMan.Spawn(id);
                Pair.Client.EntMan.RunMapInit(dummy, Pair.Client.MetaData(dummy));

                var sprite = Pair.Client.EntMan.GetComponentOrNull<SpriteComponent>(dummy);
                Assert.That(sprite, Is.Not.Null, $"{id}: нет компонента спрайта.");

                foreach (var layer in sprite!.AllLayers)
                {
                    Assert.That(layer.Texture != null || layer.ActualRsi != null,
                        $"{id}: слой спрайта не нашёл текстуру.");
                }

                Pair.Client.EntMan.DeleteEntity(dummy);
            }
        });
    }

    [Test]
    public async Task PlasmaGrindsIntoDust()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(SProtoMan.HasIndex<ReagentPrototype>("ADTPlasmaDust"), "Нет реагента плазменной пыли.");

            var sheet = SEntMan.SpawnEntity("SheetPlasma1", map.GridCoords);
            Assert.That(_solution.TryGetSolution(sheet, "composite", out _, out var solution),
                "У листа плазмы нет перемалываемого раствора.");

            var plasma = solution!.GetTotalPrototypeQuantity("Plasma");
            var dust = solution.GetTotalPrototypeQuantity("ADTPlasmaDust");

            Assert.That(dust, Is.GreaterThan(FixedPoint2.Zero), "Перемолка плазмы не даёт пыли.");
            Assert.That(dust, Is.EqualTo(plasma), "Перемолка должна давать плазму и пыль поровну.");

            SEntMan.DeleteEntity(sheet);
        });
    }

    [Test]
    public async Task StageGrowsOneStepPerTick()
    {
        var map = await Pair.CreateTestMap();
        var mobs = new List<EntityUid>();
        ADTDisease? tracked = null;

        await Pair.Server.WaitAssertion(() =>
        {
            for (var i = 0; i < 4; i++)
            {
                var mob = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
                mobs.Add(mob);
            }

            var disease = ADTDisease.FromPrototype(SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseKingstons"));
            disease.StageProb = 100f;
            disease.MutationChance = 0f;

            tracked = _disease.Contract(mobs[0], disease);
            Assert.That(tracked, Is.Not.Null, "Нулевой пациент не заразился.");
            tracked!.StageProb = 100f;
            tracked.MutationChance = 0f;
        });

        var previous = 1;
        for (var tick = 0; tick < 6; tick++)
        {
            await Pair.RunSeconds(2.5f);

            await Pair.Server.WaitAssertion(() =>
            {
                var stage = tracked!.Stage;
                Assert.That(stage - previous, Is.LessThanOrEqualTo(1),
                    $"Стадия выросла больше чем на одну за тик: было {previous}, стало {stage}.");
                previous = stage;
            });
        }

        await Pair.Server.WaitAssertion(() =>
        {
            foreach (var mob in mobs)
            {
                SEntMan.DeleteEntity(mob);
            }
        });
    }

    [Test]
    public async Task BloodDataExposesVirus()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            var mob = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.That(_disease.Contract(mob, ADTDisease.FromPrototype(
                SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseCold"))), Is.Not.Null);

            var blood = SEntMan.GetComponent<BloodstreamComponent>(mob);
            Assert.That(blood.BloodData, Is.Not.Null, "У крови нет данных.");

            var virus = blood.BloodData!.OfType<ADTVirusData>().FirstOrDefault();
            Assert.That(virus, Is.Not.Null, "В BloodData нет данных о вирусах.");
            Assert.That(virus!.DiseaseIds, Does.Contain("ADTDiseaseCold"), "Сводка болезней пустая.");

            SEntMan.DeleteEntity(mob);
        });
    }

    [Test]
    public async Task FoodCuresThroughStomach()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitAssertion(() =>
        {
            var mob = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var disease = _disease.Contract(mob, ADTDisease.FromPrototype(
                SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseKingstons")));

            Assert.That(disease, Is.Not.Null);
            Assert.That(disease!.Cures, Does.Contain(new ProtoId<ReagentPrototype>("Milk")),
                "Синдром Кингстона должен лечиться молоком.");
            Assert.That(_disease.HasCure(mob, disease), Is.False, "Лекарство нашлось до приёма.");

            var body = SEntMan.GetComponent<BodyComponent>(mob);
            Assert.That(_body.TryGetOrgansWithComponent<StomachComponent>((mob, body), out var stomachs));
            Assert.That(stomachs, Is.Not.Empty, "У моба нет желудка.");

            foreach (var stomach in stomachs)
            {
                _stomach.TryTransferSolution((stomach.Owner, stomach.Comp, null), new Solution("Milk", 10));
            }

            Assert.That(_disease.HasCure(mob, disease), "Молоко в желудке не считается лекарством.");

            var medicine = _disease.Contract(mob, ADTDisease.FromPrototype(
                SProtoMan.Index<ADTDiseasePrototype>("ADTDiseaseCold")));
            Assert.That(medicine, Is.Not.Null);
            Assert.That(_disease.GetDigestedReagentAmount(mob, "ADTSpaceacillin"), Is.EqualTo(FixedPoint2.Zero),
                "Лекарства не должны работать через желудок.");

            SEntMan.DeleteEntity(mob);
        });
    }
}
