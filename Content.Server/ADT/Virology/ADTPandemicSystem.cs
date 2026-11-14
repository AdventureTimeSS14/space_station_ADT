using System.Linq;
using System.Text;
using Content.Server.Popups;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Pandemic;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Virology;

public sealed class ADTPandemicSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ADTDiseaseSystem _disease = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private LabelSystem _label = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTPandemicComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<ADTPandemicComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<ADTPandemicComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ADTPandemicComponent, ADTPandemicEjectBeakerMessage>(OnEjectBeaker);
        SubscribeLocalEvent<ADTPandemicComponent, ADTPandemicRenameDiseaseMessage>(OnRenameDisease);
        SubscribeLocalEvent<ADTPandemicComponent, ADTPandemicCreateCultureMessage>(OnCreateCulture);
        SubscribeLocalEvent<ADTPandemicComponent, ADTPandemicCreateVaccineMessage>(OnCreateVaccine);
        SubscribeLocalEvent<ADTPandemicComponent, ADTPandemicPrintFormMessage>(OnPrintForm);
        SubscribeLocalEvent<ADTPandemicComponent, ADTPandemicRequestSymptomsMessage>(OnRequestSymptoms);
        SubscribeLocalEvent<ADTPandemicComponent, ADTPandemicAddSymptomMessage>(OnAddSymptom);
        SubscribeLocalEvent<ADTPandemicComponent, ADTPandemicRemoveSymptomMessage>(OnRemoveSymptom);
    }

    private void OnContainerModified(Entity<ADTPandemicComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.BeakerSlotId)
            return;

        OnBeakerChanged(ent);
    }

    private void OnContainerModified(Entity<ADTPandemicComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.BeakerSlotId)
            return;

        OnBeakerChanged(ent);
    }

    private void OnBeakerChanged(Entity<ADTPandemicComponent> ent)
    {
        ent.Comp.Candidates.Clear();
        ent.Comp.CandidatesForDisease = -1;

        if (TryGetSample(ent, out _, out var data) && data != null)
        {
            data.Resistances.RemoveWhere(id =>
                _proto.TryIndex<ADTDiseasePrototype>(id, out var proto) && proto.NoVaccine);
        }

        _appearance.SetData(ent, ADTPandemicVisuals.HasBeaker, TryGetBeaker(ent, out _));
        UpdateUi(ent);
    }

    private void OnUiOpened(Entity<ADTPandemicComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private bool TryGetBeaker(Entity<ADTPandemicComponent> ent, out EntityUid beaker)
    {
        beaker = default;
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.BeakerSlotId, out var slot) || slot.Item == null)
            return false;

        beaker = slot.Item.Value;
        return true;
    }

    private bool TryGetSample(Entity<ADTPandemicComponent> ent, out Solution? solution, out ADTVirusData? data)
    {
        solution = null;
        data = null;

        if (!TryGetBeaker(ent, out var beaker))
            return false;

        if (!_solution.TryGetDrainableSolution(beaker, out _, out solution))
            return false;

        data = ADTDiseaseSystem.FindVirusData(solution, out _);
        return true;
    }

    private bool IsBusy(Entity<ADTPandemicComponent> ent)
    {
        return _timing.CurTime < ent.Comp.BusyUntil;
    }

    private void StartCooldown(Entity<ADTPandemicComponent> ent, TimeSpan time)
    {
        ent.Comp.BusyUntil = _timing.CurTime + time;
        _appearance.SetData(ent, ADTPandemicVisuals.Busy, true);

        if (ent.Comp.WorkingSoundEntity == null && ent.Comp.WorkingSound is { } working)
            ent.Comp.WorkingSoundEntity = _audio.PlayPvs(working, ent.Owner, working.Params.WithLoop(true));

        UpdateUi(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ADTPandemicComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.BusyUntil == TimeSpan.Zero || _timing.CurTime < comp.BusyUntil)
                continue;

            comp.BusyUntil = TimeSpan.Zero;
            _appearance.SetData(uid, ADTPandemicVisuals.Busy, false);

            if (comp.WorkingSoundEntity is { } sound)
            {
                _audio.Stop(sound.Owner, sound.Comp);
                comp.WorkingSoundEntity = null;
            }

            FinishPending((uid, comp));

            _audio.PlayPvs(comp.ReadySound, uid);
            UpdateUi((uid, comp));
        }
    }

    private void FinishPending(Entity<ADTPandemicComponent> ent)
    {
        if (ent.Comp.PendingCulture is { } culture)
        {
            ent.Comp.PendingCulture = null;

            var bottle = Spawn(ent.Comp.CultureBottle, _transform.GetMapCoordinates(ent.Owner));
            if (_solution.TryGetDrainableSolution(bottle, out var soln, out _))
            {
                var data = new ADTVirusData();
                data.Diseases.Add(culture);
                _solution.TryAddReagent(soln.Value, new ReagentId("Blood", new List<ReagentData> { data }), 20, out _);
            }

            if (ent.Comp.PendingCultureName is { } cultureName)
                _label.Label(bottle, Loc.GetString("adt-pandemic-culture-label", ("name", cultureName)));

            ent.Comp.PendingCultureName = null;
        }

        if (ent.Comp.PendingVaccine is { } id)
        {
            ent.Comp.PendingVaccine = null;

            var bottle = Spawn(ent.Comp.VaccineBottle, _transform.GetMapCoordinates(ent.Owner));
            if (_solution.TryGetDrainableSolution(bottle, out var soln, out _))
            {
                var vaccine = new ADTVaccineData();
                vaccine.Ids.Add(id);
                _solution.TryAddReagent(soln.Value, new ReagentId("ADTVaccine", new List<ReagentData> { vaccine }), 15, out _);
            }

            if (ent.Comp.PendingVaccineName is { } vaccineName)
                _label.Label(bottle, Loc.GetString("adt-pandemic-vaccine-label", ("name", vaccineName)));

            ent.Comp.PendingVaccineName = null;
        }
    }

    public void UpdateUi(Entity<ADTPandemicComponent> ent)
    {
        var state = new ADTPandemicBoundUserInterfaceState
        {
            Busy = IsBusy(ent),
        };

        if (!TryGetBeaker(ent, out var beaker))
        {
            state.ErrorMessage = "adt-pandemic-error-no-beaker";
            _ui.SetUiState(ent.Owner, ADTPandemicUiKey.Key, state);
            return;
        }

        state.HasBeaker = true;

        if (!_solution.TryGetDrainableSolution(beaker, out _, out var solution) || solution.Volume <= 0)
        {
            state.ErrorMessage = "adt-pandemic-error-empty";
            _ui.SetUiState(ent.Owner, ADTPandemicUiKey.Key, state);
            return;
        }

        var data = ADTDiseaseSystem.FindVirusData(solution, out var carrier);
        if (data == null)
        {
            state.ErrorMessage = "adt-pandemic-error-no-blood";
            _ui.SetUiState(ent.Owner, ADTPandemicUiKey.Key, state);
            return;
        }

        state.BloodDna = carrier.Reagent.Data?
            .OfType<DnaData>()
            .FirstOrDefault()?.DNA ?? Loc.GetString("adt-pandemic-unknown");

        FillDiseases(state, data.Diseases, data.Resistances, false);

        if (ent.Comp.CandidatesForDisease >= 0)
        {
            state.CandidatesForDisease = ent.Comp.CandidatesForDisease;
            foreach (var candidate in ent.Comp.Candidates)
            {
                if (_proto.TryIndex(candidate, out var symptomProto))
                    state.SymptomCandidates.Add((candidate.Id, Loc.GetString(symptomProto.Name)));
            }
        }

        _ui.SetUiState(ent.Owner, ADTPandemicUiKey.Key, state);
    }

    public void FillDiseases(ADTPandemicBoundUserInterfaceState state, IEnumerable<ADTDisease> diseases, IEnumerable<string> resistances, bool ignoreStealth)
    {
        var index = 0;
        foreach (var disease in diseases)
        {
            index++;

            if (!ignoreStealth && (disease.Visibility & ADTDiseaseVisibility.HiddenPandemic) != 0)
                continue;

            if (!_proto.TryIndex(disease.Prototype, out var proto))
                continue;

            var info = new ADTPandemicDiseaseInfo
            {
                Index = index - 1,
                Name = _disease.GetDiseaseName(disease),
                Agent = Loc.GetString(proto.Agent),
                Description = proto.Description is { } desc ? Loc.GetString(desc) : string.Empty,
                Route = _disease.GetSpreadText(disease),
                Cure = _disease.GetCureText(disease),
                CanImmunity = proto.CanImmunity,
                IsAdvance = disease.IsAdvance,
            };

            foreach (var symptom in disease.Symptoms)
            {
                if (_proto.TryIndex(symptom, out var symptomProto))
                    info.Symptoms.Add((symptom.Id, Loc.GetString(symptomProto.Name)));
            }

            state.Diseases.Add(info);
        }

        var antibodyIndex = 0;
        foreach (var id in resistances)
        {
            antibodyIndex++;
            var name = Loc.GetString("adt-pandemic-unknown");
            var canVaccine = true;

            if (_proto.TryIndex<ADTDiseasePrototype>(id, out var proto))
            {
                name = Loc.GetString(proto.Name);
                canVaccine = !proto.NoVaccine;
            }
            else if (_disease.GetArchived(id) is { } archived)
            {
                name = archived.Name ?? Loc.GetString("adt-disease-unknown");
            }

            state.Antibodies.Add(new ADTPandemicAntibodyInfo
            {
                Index = antibodyIndex - 1,
                Name = name,
                CanVaccine = canVaccine,
            });
        }
    }

    private ADTDisease? GetDiseaseByIndex(Entity<ADTPandemicComponent> ent, int index)
    {
        if (!TryGetSample(ent, out _, out var data) || data == null)
            return null;

        if (index < 0 || index >= data.Diseases.Count)
            return null;

        return data.Diseases[index];
    }

    private void OnEjectBeaker(Entity<ADTPandemicComponent> ent, ref ADTPandemicEjectBeakerMessage args)
    {
        if (!TryGetBeaker(ent, out var beaker))
            return;

        if (args.Clear && _solution.TryGetDrainableSolution(beaker, out var soln, out _))
            _solution.RemoveAllSolution(soln.Value);

        if (_itemSlots.TryGetSlot(ent.Owner, ent.Comp.BeakerSlotId, out var slot))
            _itemSlots.TryEjectToHands(ent.Owner, slot, args.Actor);
        UpdateUi(ent);
    }

    private void OnRenameDisease(Entity<ADTPandemicComponent> ent, ref ADTPandemicRenameDiseaseMessage args)
    {
        var disease = GetDiseaseByIndex(ent, args.Index);
        if (disease == null || !disease.IsAdvance)
            return;

        var name = args.Name.Trim();
        if (name.Length == 0)
            return;

        if (name.Length > 32)
            name = name[..32];

        _disease.SetArchivedName(disease.GetId(), name);
        disease.Name = name;
        UpdateUi(ent);
    }

    private void OnCreateCulture(Entity<ADTPandemicComponent> ent, ref ADTPandemicCreateCultureMessage args)
    {
        if (IsBusy(ent))
            return;

        var disease = GetDiseaseByIndex(ent, args.Index);
        if (disease == null)
            return;

        var copy = disease.Copy();
        copy.Name = disease.Name;

        ent.Comp.PendingCulture = copy;
        ent.Comp.PendingCultureName = _disease.GetDiseaseName(disease);
        StartCooldown(ent, ent.Comp.CultureCooldown);
    }

    private void OnCreateVaccine(Entity<ADTPandemicComponent> ent, ref ADTPandemicCreateVaccineMessage args)
    {
        if (IsBusy(ent))
            return;

        if (!TryGetSample(ent, out _, out var data) || data == null)
            return;

        var resistances = data.Resistances.ToList();
        if (args.Index < 0 || args.Index >= resistances.Count)
            return;

        var id = resistances[args.Index];
        if (_proto.TryIndex<ADTDiseasePrototype>(id, out var proto) && proto.NoVaccine)
            return;

        var name = _proto.TryIndex<ADTDiseasePrototype>(id, out var namedProto)
            ? Loc.GetString(namedProto.Name)
            : _disease.GetArchived(id)?.Name ?? Loc.GetString("adt-disease-unknown");

        ent.Comp.PendingVaccine = id;
        ent.Comp.PendingVaccineName = name;
        StartCooldown(ent, ent.Comp.VaccineCooldown);
    }

    private void OnPrintForm(Entity<ADTPandemicComponent> ent, ref ADTPandemicPrintFormMessage args)
    {
        if (IsBusy(ent))
            return;

        var disease = GetDiseaseByIndex(ent, args.Index);
        if (disease == null)
            return;

        var symptoms = new List<string>();
        foreach (var symptom in disease.Symptoms)
        {
            if (_proto.TryIndex(symptom, out var symptomProto))
                symptoms.Add(Loc.GetString(symptomProto.Name));
        }

        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("adt-pandemic-form-header"));
        builder.AppendLine(Loc.GetString("adt-pandemic-form-name", ("name", _disease.GetDiseaseName(disease))));
        builder.AppendLine(Loc.GetString("adt-pandemic-form-symptoms",
            ("symptoms", symptoms.Count > 0 ? string.Join(", ", symptoms) : Loc.GetString("adt-pandemic-form-none"))));
        builder.AppendLine(Loc.GetString("adt-pandemic-form-route", ("route", _disease.GetSpreadText(disease))));
        builder.AppendLine(Loc.GetString("adt-pandemic-form-cure", ("cure", _disease.GetCureText(disease))));
        builder.AppendLine();
        builder.AppendLine(Loc.GetString("adt-pandemic-form-responsibility"));

        var paper = Spawn(ent.Comp.FormPaper, _transform.GetMapCoordinates(ent.Owner));
        if (TryComp<PaperComponent>(paper, out var paperComp))
            _paper.SetContent((paper, paperComp), builder.ToString());

        _label.Label(paper, Loc.GetString("adt-pandemic-form-label", ("name", _disease.GetDiseaseName(disease))));
        _audio.PlayPvs(ent.Comp.PrintSound, ent.Owner);
    }

    private void OnRequestSymptoms(Entity<ADTPandemicComponent> ent, ref ADTPandemicRequestSymptomsMessage args)
    {
        var disease = GetDiseaseByIndex(ent, args.Index);
        if (disease == null || !disease.IsAdvance)
            return;

        ent.Comp.Candidates = _disease.GenerateSymptoms(
            disease,
            levelMax: ent.Comp.MaxSymptomLevel,
            count: ent.Comp.SymptomChoices);

        ent.Comp.CandidatesForDisease = args.Index;
        UpdateUi(ent);
    }

    private void OnAddSymptom(Entity<ADTPandemicComponent> ent, ref ADTPandemicAddSymptomMessage args)
    {
        var disease = GetDiseaseByIndex(ent, args.Index);
        if (disease == null || !disease.IsAdvance)
            return;

        if (!ent.Comp.Candidates.Contains(args.SymptomId))
            return;

        if (!TryGetBeaker(ent, out var beaker) || !_solution.TryGetDrainableSolution(beaker, out var soln, out _))
            return;

        var symptom = args.SymptomId;
        _disease.ModifySolutionAdvance(soln.Value, target =>
        {
            _disease.AddSymptom(target, symptom);
            _disease.RefreshAdvance(target, resetName: true);
        });

        ent.Comp.Candidates.Clear();
        ent.Comp.CandidatesForDisease = -1;
        UpdateUi(ent);
    }

    private void OnRemoveSymptom(Entity<ADTPandemicComponent> ent, ref ADTPandemicRemoveSymptomMessage args)
    {
        var disease = GetDiseaseByIndex(ent, args.Index);
        if (disease == null || !disease.IsAdvance || disease.Symptoms.Count <= 1)
            return;

        if (!TryGetBeaker(ent, out var beaker) || !_solution.TryGetDrainableSolution(beaker, out var soln, out _))
            return;

        var symptom = args.SymptomId;
        _disease.ModifySolutionAdvance(soln.Value, target =>
        {
            if (target.Symptoms.Count <= 1)
                return;

            _disease.RemoveSymptom(target, symptom);
            _disease.RefreshAdvance(target, resetName: true);
        });

        UpdateUi(ent);
    }
}
