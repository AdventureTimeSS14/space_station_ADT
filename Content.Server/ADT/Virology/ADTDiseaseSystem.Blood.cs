using System.Linq;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Server.ADT.Virology;

public sealed partial class ADTDiseaseSystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;

    private bool _scanningFluids;

    private void InitializeBlood()
    {
        SubscribeLocalEvent<BloodstreamComponent, ReactionEntityEvent>(OnReactionEntity);
        SubscribeLocalEvent<BloodstreamComponent, GetReagentEffectsEvent>(OnGetReagentEffects);
        SubscribeLocalEvent<StomachComponent, SolutionChangedEvent>(OnStomachSolutionChanged);
    }

    private void OnStomachSolutionChanged(Entity<StomachComponent> ent, ref SolutionChangedEvent args)
    {
        if (GetOrganBody(ent.Owner) is not { } body)
            return;

        ScanFluids(body, args.Solution, allowVaccine: false);
    }

    private EntityUid? GetOrganBody(EntityUid organ)
    {
        var current = Transform(organ).ParentUid;
        for (var i = 0; i < 4 && current.IsValid(); i++)
        {
            if (HasComp<BodyComponent>(current))
                return current;

            current = Transform(current).ParentUid;
        }

        return null;
    }

    public void ScanFluids(EntityUid uid, Entity<SolutionComponent> solution, bool allowVaccine)
    {
        if (_scanningFluids)
            return;

        _scanningFluids = true;
        try
        {
            foreach (var reagent in solution.Comp.Solution.Contents.ToArray())
            {
                var data = reagent.Reagent.Data;
                if (data == null || data.Count == 0)
                    continue;

                foreach (var entry in data.ToList())
                {
                    switch (entry)
                    {
                        case ADTVirusData virus:
                            InfectFromBlood(uid, virus, ReactionMethod.Injection);
                            break;
                        case ADTVaccineData vaccine when allowVaccine:
                            ApplyVaccine(uid, vaccine, ReactionMethod.Injection);
                            break;
                    }
                }
            }
        }
        finally
        {
            _scanningFluids = false;
        }
    }

    private void OnReactionEntity(Entity<BloodstreamComponent> ent, ref ReactionEntityEvent args)
    {
        var data = args.ReagentQuantity.Reagent.Data;
        if (data == null || data.Count == 0)
            return;

        foreach (var entry in data.ToList())
        {
            switch (entry)
            {
                case ADTVirusData virus:
                    InfectFromBlood(ent, virus, args.Method);
                    break;
                case ADTVaccineData vaccine:
                    ApplyVaccine(ent, vaccine, args.Method);
                    break;
            }
        }
    }

    private void OnGetReagentEffects(Entity<BloodstreamComponent> ent, ref GetReagentEffectsEvent args)
    {
        var data = args.Reagent.Data;
        if (data == null || data.Count == 0)
            return;

        foreach (var entry in data.ToList())
        {
            switch (entry)
            {
                case ADTVirusData virus:
                    InfectFromBlood(ent, virus, ReactionMethod.Injection);
                    break;
            }
        }
    }

    private void InfectFromBlood(EntityUid uid, ADTVirusData virus, ReactionMethod method)
    {
        foreach (var disease in virus.Diseases.ToList())
        {
            if (!_proto.TryIndex(disease.Prototype, out var proto) || !proto.IsVirus)
                continue;

            if (method == ReactionMethod.Touch)
                Contract(uid, disease, ADTDiseaseSpread.Contact, protectionCheck: true);
            else
                Contract(uid, disease, ADTDiseaseSpread.Blood);
        }
    }

    private void ApplyVaccine(EntityUid uid, ADTVaccineData vaccine, ReactionMethod method)
    {
        if (method != ReactionMethod.Injection)
            return;

        if (TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier))
        {
            foreach (var disease in carrier.Diseases.ToList())
            {
                if (vaccine.Ids.Contains(disease.GetId()))
                    Cure((uid, carrier), disease);
            }
        }

        AddResistances(uid, vaccine.Ids);
    }

    public ADTVirusData? GetCarrierVirusData(EntityUid uid)
    {
        if (!TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier))
            return null;

        if (carrier.Diseases.Count == 0 && carrier.Resistances.Count == 0)
            return null;

        return new ADTVirusData
        {
            Diseases = carrier.Diseases.Select(d => d.Copy()).ToList(),
            Resistances = new HashSet<string>(carrier.Resistances),
        };
    }

    public void UpdateBloodData(EntityUid uid)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var blood))
            return;

        var scanning = _scanningFluids;
        _scanningFluids = true;
        try
        {
            UpdateBloodDataCore(uid, blood);
        }
        finally
        {
            _scanningFluids = scanning;
        }
    }

    private void UpdateBloodDataCore(EntityUid uid, BloodstreamComponent blood)
    {
        var virusData = GetCarrierVirusData(uid);
        var baseData = _bloodstream.GetEntityBloodData((uid, blood))
            .Where(d => d is not ADTVirusData)
            .Select(d => d.Clone())
            .ToList();

        if (virusData == null && baseData.Count == (blood.BloodData?.Count ?? 0) && blood.BloodData?.All(d => d is not ADTVirusData) == true)
            return;

        var newData = new List<ReagentData>(baseData);
        if (virusData != null)
            newData.Add(virusData);

        _bloodstream.SetBloodDataVirology((uid, blood), newData);
    }

    private static List<ReagentData> CloneData(List<ReagentData> data)
    {
        return data.Select(d => d.Clone()).ToList();
    }

    public static ADTVirusData? GetVirusData(ReagentQuantity reagent)
    {
        return reagent.Reagent.Data?.OfType<ADTVirusData>().FirstOrDefault();
    }

    public static ADTVirusData? FindVirusData(Solution solution, out ReagentQuantity carrierReagent)
    {
        foreach (var reagent in solution.Contents)
        {
            var data = GetVirusData(reagent);
            if (data == null)
                continue;

            carrierReagent = reagent;
            return data;
        }

        carrierReagent = default;
        return null;
    }
}
