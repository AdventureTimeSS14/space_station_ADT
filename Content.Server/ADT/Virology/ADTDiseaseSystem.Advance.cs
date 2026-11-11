using System.Linq;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed partial class ADTDiseaseSystem
{
    public const int SymptomLimit = 6;
    public const int MaxSymptomLevel = 6;

    public static readonly ProtoId<ADTDiseasePrototype> AdvancePrototype = "ADTAdvanceVirus";

    public static readonly ProtoId<ReagentPrototype>[] AdvanceCures =
    {
        "TableSalt",
        "Sugar",
        "JuiceOrange",
        "ADTSpaceacillin",
        "Saline",
        "Ethanol",
        "ADTTeporone",
        "Diphenhydramine",
        "Lipolicide",
        "Silver",
        "Gold",
    };

    private readonly Dictionary<string, ADTDisease> _archive = new();

    private bool _mixing;

    private void InitializeAdvance()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<SolutionContainerManagerComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _archive.Clear();
    }

    public ADTDisease? GetArchived(string id)
    {
        return _archive.GetValueOrDefault(id);
    }

    public void SetArchivedName(string id, string name)
    {
        if (_archive.TryGetValue(id, out var archived))
            archived.Name = name;
    }

    public ADTDisease CreateAdvance(List<ProtoId<ADTSymptomPrototype>>? symptoms = null)
    {
        var disease = ADTDisease.FromPrototype(_proto.Index(AdvancePrototype));
        disease.Symptoms = symptoms ?? new List<ProtoId<ADTSymptomPrototype>>();

        if (disease.Symptoms.Count == 0)
            disease.Symptoms = GenerateSymptoms(disease, 1, 2);

        AssignProperties(disease, GenerateProperties(disease));
        return disease;
    }

    public ADTDisease CreatePresetAdvance(ADTDiseasePrototype proto)
    {
        var disease = CreateAdvance(proto.Symptoms.ToList());
        disease.Name = Loc.GetString(proto.Name);
        return disease;
    }

    public bool HasSymptom(ADTDisease disease, ProtoId<ADTSymptomPrototype> symptom)
    {
        return disease.Symptoms.Contains(symptom);
    }

    public List<ProtoId<ADTSymptomPrototype>> GenerateSymptoms(
        ADTDisease disease,
        int levelMin = 1,
        int levelMax = MaxSymptomLevel,
        int count = 0,
        bool overrideSymptoms = false)
    {
        var generated = new List<ProtoId<ADTSymptomPrototype>>();
        var possible = new List<ProtoId<ADTSymptomPrototype>>();

        foreach (var symptom in _proto.EnumeratePrototypes<ADTSymptomPrototype>())
        {
            if (!symptom.Generatable)
                continue;

            if (symptom.Level < levelMin || symptom.Level > levelMax)
                continue;

            if (HasSymptom(disease, symptom.ID) && !overrideSymptoms)
                continue;

            possible.Add(symptom.ID);
        }

        if (possible.Count == 0)
            return generated;

        var n = 1;
        if (count > 0)
        {
            n = count;
        }
        else
        {
            while (_random.Prob(0.2f) && n < SymptomLimit)
            {
                n++;
            }
        }

        for (var i = 0; i < n && possible.Count > 0; i++)
        {
            generated.Add(_random.PickAndTake(possible));
        }

        return generated;
    }

    public ADTDiseaseProperties GenerateProperties(ADTDisease disease)
    {
        var properties = new ADTDiseaseProperties
        {
            Resistance = 1,
            Stealth = 0,
            StageSpeed = 1,
            Transmittable = 1,
            Severity = 0,
        };

        foreach (var id in disease.Symptoms)
        {
            if (!_proto.TryIndex(id, out var symptom))
                continue;

            properties.Resistance += symptom.Resistance;
            properties.Stealth += symptom.Stealth;
            properties.StageSpeed += symptom.StageSpeed;
            properties.Transmittable += symptom.Transmittable;
            properties.Severity = Math.Max(properties.Severity, symptom.Severity);
        }

        return properties;
    }

    public void AssignProperties(ADTDisease disease, ADTDiseaseProperties properties)
    {
        disease.Visibility = properties.Stealth switch
        {
            1 => ADTDiseaseVisibility.HiddenHud,
            2 => ADTDiseaseVisibility.HiddenHud | ADTDiseaseVisibility.HiddenScanner,
            >= 3 => ADTDiseaseVisibility.HiddenHud | ADTDiseaseVisibility.HiddenScanner | ADTDiseaseVisibility.HiddenPandemic,
            _ => ADTDiseaseVisibility.Visible,
        };

        var transmit = properties.Transmittable - disease.Symptoms.Count / 2;
        disease.Spread = transmit switch
        {
            <= 1 => ADTDiseaseSpread.Blood,
            <= 3 => ADTDiseaseSpread.Blood | ADTDiseaseSpread.Contact,
            _ => ADTDiseaseSpread.Blood | ADTDiseaseSpread.Airborne,
        };

        disease.Permeability = Math.Clamp(0.25f * properties.Transmittable, 0.2f, 2f);

        var sqrt = properties.StageSpeed + 11 > 0
            ? MathF.Sqrt(properties.StageSpeed + 11)
            : 0f;
        disease.StageProb = Math.Clamp(Math.Max(1.3f * sqrt, properties.StageSpeed), 1f, 40f);

        disease.Severity = (ADTDiseaseSeverity) Math.Clamp(properties.Severity, 0, (int) ADTDiseaseSeverity.Biohazard);

        disease.CureProb = Math.Clamp(15 - properties.Resistance, 5, 40);
        GenerateCure(disease, properties.Resistance);
    }

    private void GenerateCure(ADTDisease disease, int resistance)
    {
        var res = (int) Math.Floor(Math.Clamp(resistance - disease.Symptoms.Count / 2f, 1f, AdvanceCures.Length));
        disease.Cures = new List<ProtoId<ReagentPrototype>> { AdvanceCures[res - 1] };
        disease.NeedsAllCures = true;
    }

    public void RefreshAdvance(ADTDisease disease, bool resetName = false, bool updateProperties = true)
    {
        if (updateProperties)
            AssignProperties(disease, GenerateProperties(disease));

        var id = disease.GetId();
        var archived = GetArchived(id);

        var source = archived ?? ADTDisease.FromPrototype(_proto.Index(AdvancePrototype));
        disease.MutationReagents = source.MutationReagents.ToList();
        disease.PossibleMutations = source.PossibleMutations.ToList();

        if (archived != null)
        {
            disease.Name = archived.Name;
            return;
        }

        if (resetName)
            disease.Name = null;

        var copy = disease.Copy();
        copy.Name = disease.Name;
        _archive[id] = copy;
    }

    public void AddSymptom(ADTDisease disease, ProtoId<ADTSymptomPrototype> symptom)
    {
        if (HasSymptom(disease, symptom))
            return;

        if (disease.Symptoms.Count < SymptomLimit - 1 + _random.Next(-1, 2))
        {
            disease.Symptoms.Add(symptom);
            return;
        }

        disease.Symptoms.Remove(_random.Pick(disease.Symptoms));
        disease.Symptoms.Add(symptom);
    }

    public void RemoveSymptom(ADTDisease disease, ProtoId<ADTSymptomPrototype> symptom)
    {
        disease.Symptoms.Remove(symptom);
    }

    public void Evolve(ADTDisease disease, int levelMin, int levelMax)
    {
        var generated = GenerateSymptoms(disease, levelMin, levelMax, 1);
        if (generated.Count == 0)
            return;

        AddSymptom(disease, _random.Pick(generated));
        RefreshAdvance(disease, resetName: true);
    }

    public void Devolve(ADTDisease disease)
    {
        if (disease.Symptoms.Count <= 1)
            return;

        RemoveSymptom(disease, _random.Pick(disease.Symptoms));
        RefreshAdvance(disease, resetName: true);
    }

    public void Mix(ADTDisease disease, ADTDisease other)
    {
        if (disease.IsSame(other))
            return;

        var symptoms = other.Symptoms.ToList();
        _random.Shuffle(symptoms);
        foreach (var symptom in symptoms)
        {
            AddSymptom(disease, symptom);
        }
    }

    public ADTDisease? AdvanceMix(IEnumerable<ADTDisease> list)
    {
        var diseases = list.Where(d => d.IsAdvance).Select(CopyAdvance).ToList();
        if (diseases.Count == 0)
            return null;

        if (diseases.Count == 1)
            return diseases[0];

        var i = 0;
        while (i < 20 && diseases.Count > 1)
        {
            i++;
            var first = _random.PickAndTake(diseases);
            var second = _random.Pick(diseases);
            Mix(second, first);
        }

        var result = _random.Pick(diseases);
        RefreshAdvance(result, resetName: true);
        return result;
    }

    private static ADTDisease CopyAdvance(ADTDisease disease)
    {
        var copy = disease.Copy();
        copy.Name = disease.Name;
        return copy;
    }

    public ADTDisease CreateRandomVirus(int resistance, int stealth, int stageSpeed, int transmittable, int severity, string? name = null)
    {
        var disease = ADTDisease.FromPrototype(_proto.Index(AdvancePrototype));
        disease.Name = name;
        disease.Symptoms = GenerateSymptoms(disease, count: _random.Next(4, 7), overrideSymptoms: true);
        AssignProperties(disease, new ADTDiseaseProperties
        {
            Resistance = resistance,
            Stealth = stealth,
            StageSpeed = stageSpeed,
            Transmittable = transmittable,
            Severity = severity,
        });
        RefreshAdvance(disease, updateProperties: false);
        return disease;
    }

    public bool ModifySolutionAdvance(Entity<SolutionComponent> solution, Action<ADTDisease> modify)
    {
        var sol = solution.Comp.Solution;
        foreach (var reagent in sol.Contents.ToList())
        {
            var data = GetVirusData(reagent);
            var advance = data?.Diseases.FirstOrDefault(d => d.IsAdvance);
            if (data == null || advance == null)
                continue;

            var newData = (ADTVirusData) data.Clone();
            var target = newData.Diseases.First(d => d.IsAdvance);
            target.Name = advance.Name;
            modify(target);

            ReplaceReagentData(sol, reagent, data, newData);
            return true;
        }

        return false;
    }

    private static void ReplaceReagentData(Solution solution, ReagentQuantity reagent, ADTVirusData oldData, ADTVirusData newData)
    {
        var list = reagent.Reagent.EnsureReagentData()
            .Where(d => !ReferenceEquals(d, oldData))
            .Select(d => d.Clone())
            .ToList();
        list.Add(newData);

        solution.RemoveReagent(reagent);
        solution.AddReagent(new ReagentId(reagent.Reagent.Prototype, list), reagent.Quantity);
    }

    private void OnSolutionChanged(Entity<SolutionContainerManagerComponent> ent, ref SolutionChangedEvent args)
    {
        if (_mixing)
            return;

        if (HasComp<BloodstreamComponent>(ent))
        {
            ScanFluids(ent.Owner, args.Solution, allowVaccine: true);
            return;
        }

        var solution = args.Solution.Comp.Solution;
        string? firstId = null;
        var needsMix = false;
        foreach (var reagent in solution.Contents)
        {
            var data = GetVirusData(reagent);
            if (data == null)
                continue;

            foreach (var disease in data.Diseases)
            {
                if (!disease.IsAdvance)
                    continue;

                var id = disease.GetId();
                if (firstId == null)
                {
                    firstId = id;
                    continue;
                }

                if (firstId != id)
                {
                    needsMix = true;
                    break;
                }
            }

            if (needsMix)
                break;
        }

        if (!needsMix)
            return;

        _mixing = true;
        try
        {
            MergeSolutionViruses(args.Solution);
        }
        finally
        {
            _mixing = false;
        }
    }

    private void MergeSolutionViruses(Entity<SolutionComponent> solution)
    {
        var sol = solution.Comp.Solution;
        var carriers = sol.Contents.Where(r => GetVirusData(r) != null).ToList();
        var all = carriers.SelectMany(r => GetVirusData(r)!.Diseases).ToList();
        var resistances = carriers.SelectMany(r => GetVirusData(r)!.Resistances).ToHashSet();

        var preserve = new List<ADTDisease>();
        var mixed = AdvanceMix(all.Where(d => d.IsAdvance));
        if (mixed != null)
            preserve.Add(mixed);

        foreach (var disease in all.Where(d => !d.IsAdvance))
        {
            if (preserve.Any(p => p.GetId() == disease.GetId()))
                continue;

            preserve.Add(disease.Copy());
        }

        foreach (var reagent in carriers)
        {
            var newData = new ADTVirusData
            {
                Diseases = preserve.Select(CopyAdvance).ToList(),
                Resistances = new HashSet<string>(resistances),
            };

            ReplaceReagentData(sol, reagent, GetVirusData(reagent)!, newData);
        }

        _solution.UpdateChemicals(solution, needsReactionsProcessing: false);
    }
}

public sealed class ADTDiseaseProperties
{
    public int Resistance;

    public int Stealth;

    public int StageSpeed;

    public int Transmittable;

    public int Severity;
}
