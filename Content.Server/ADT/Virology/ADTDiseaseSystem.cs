using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body;
using Content.Server.Atmos.Rotting;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.StatusEffectNew;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Virology;

public sealed partial class ADTDiseaseSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private RottingSystem _rotting = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public static readonly ProtoId<ReagentPrototype> Spaceacillin = "ADTSpaceacillin";

    public static readonly HashSet<string> DigestibleCureGroups = new() { "Foods", "Drinks", "Botanical", "Biological" };

    private readonly List<Entity<ADTDiseaseCarrierComponent>> _updateQueue = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeSpread();
        InitializeBlood();
        InitializeAdvance();

        SubscribeLocalEvent<ADTDiseaseCarrierComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnRejuvenate(Entity<ADTDiseaseCarrierComponent> ent, ref RejuvenateEvent args)
    {
        CureAll(ent.Owner, needImmunity: false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        UpdateRotting(now);
        _updateQueue.Clear();

        var query = EntityQueryEnumerator<ADTDiseaseCarrierComponent>();
        while (query.MoveNext(out var uid, out var carrier))
        {
            if (carrier.Diseases.Count == 0)
                continue;

            if (now < carrier.NextUpdate)
                continue;

            carrier.NextUpdate = now + carrier.UpdateInterval;
            _updateQueue.Add((uid, carrier));
        }

        foreach (var carrier in _updateQueue)
        {
            if (Deleted(carrier.Owner) || Terminating(carrier.Owner))
                continue;

            ProcessCarrier(carrier);
        }
    }

    private void ProcessCarrier(Entity<ADTDiseaseCarrierComponent> ent)
    {
        if (InStasis(ent))
            return;

        var dead = _mobState.IsDead(ent);
        foreach (var disease in ent.Comp.Diseases.ToList())
        {
            if (!ent.Comp.Diseases.Contains(disease))
                continue;

            if (!_proto.TryIndex(disease.Prototype, out var proto))
                continue;

            if (dead && !proto.CanProgressInDead)
                continue;

            StageAct(ent, disease, proto, dead);
        }
    }

    private void StageAct(Entity<ADTDiseaseCarrierComponent> ent, ADTDisease disease, ADTDiseasePrototype proto, bool dead)
    {
        if (proto.IsVirus && CanSpread(ent, disease, proto, dead))
            Spread(ent, disease);

        var cure = HasCure(ent.AsNullable(), disease);
        disease.Stage = Math.Min(disease.Stage, disease.MaxStages);

        if (cure)
            TryReduceStage(disease);
        else
            TryIncreaseStage(ent, disease, proto);

        if (proto.Curable && cure && _random.Prob(disease.CureProb / 100f))
        {
            Cure(ent.AsNullable(), disease);
            return;
        }

        if (disease.PossibleMutations.Count > 0 && _random.Prob(disease.MutationChance / 100f))
        {
            if (TryMutate(ent, disease))
                return;
        }

        if (proto.IsVirus)
        {
            if (disease.Carrier)
                return;

            if (proto.CarrierOnly != null && _whitelist.IsWhitelistPass(proto.CarrierOnly, ent))
                return;
        }

        if (proto.BindsMaster)
            MasterAct(ent, disease);

        if (disease.IsAdvance)
        {
            ActivateSymptoms(ent, disease);
            return;
        }

        ApplyStageEffects(ent, disease, proto);
    }

    private void TryIncreaseStage(Entity<ADTDiseaseCarrierComponent> ent, ADTDisease disease, ADTDiseasePrototype proto)
    {
        var ev = new ADTDiseaseStageIncreaseAttemptEvent(disease);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
            return;

        var prob = disease.StageProb;
        if (proto.IsVirus && HasReagent(ent, Spaceacillin))
            prob /= 2f;

        if (!ev.Forced && !_random.Prob(Math.Clamp(prob, 0f, 100f) / 100f))
            return;

        disease.Stage = Math.Min(disease.Stage + 1, disease.MaxStages);
        if (!disease.Discovered && disease.Stage >= (int) Math.Ceiling(disease.MaxStages * proto.DiscoveryThreshold))
        {
            disease.Discovered = true;
            RaiseDiseasesChanged(ent);
        }
    }

    private void TryReduceStage(ADTDisease disease)
    {
        if (_random.Prob(disease.CureProb / 100f))
            disease.Stage = Math.Max(disease.Stage - 1, 1);
    }

    private void ApplyStageEffects(EntityUid uid, ADTDisease disease, ADTDiseasePrototype proto)
    {
        foreach (var stage in proto.Stages)
        {
            if (disease.Stage < stage.MinStage || disease.Stage > stage.MaxStage)
                continue;

            _effects.ApplyEffects(uid, stage.Effects);
        }
    }

    public bool HasCure(EntityUid uid, ADTDisease disease)
    {
        var ev = new ADTDiseaseHasCureEvent(disease);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Handled)
            return ev.HasCure;

        if (disease.Cures.Count == 0)
            return false;

        var found = disease.Cures.Count(c => HasReagent(uid, c));
        var hasReagents = found > 0 && (!disease.NeedsAllCures || found >= disease.Cures.Count);

        if (!_proto.TryIndex(disease.Prototype, out var proto))
            return hasReagents;

        if (proto.RestCure)
            return HasRestCure(uid, hasReagents);

        if (!hasReagents)
            return false;

        return HasRequiredResistances(uid, proto);
    }

    private bool HasRestCure(EntityUid uid, bool hasReagents)
    {
        var sleeping = HasComp<SleepingComponent>(uid);
        var lying = _standing.IsDown(uid);

        if (hasReagents)
        {
            if (sleeping)
                return true;

            return _random.Prob(lying ? 0.33f : 0.01f);
        }

        if (sleeping)
            return _random.Prob(0.2f);

        return lying && _random.Prob(0.07f);
    }

    private bool HasRequiredResistances(EntityUid uid, ADTDiseasePrototype proto)
    {
        if (proto.RequiredResistances.Count == 0)
            return true;

        if (!TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier))
            return _random.Prob(proto.MissingResistanceCureProb / 100f);

        foreach (var required in proto.RequiredResistances)
        {
            if (!carrier.Resistances.Contains(required.Id))
                return _random.Prob(proto.MissingResistanceCureProb / 100f);
        }

        return true;
    }

    public bool HasReagent(EntityUid uid, ProtoId<ReagentPrototype> reagent, float minAmount = 0f)
    {
        return GetReagentAmount(uid, reagent) > minAmount;
    }

    public FixedPoint2 GetReagentAmount(EntityUid uid, ProtoId<ReagentPrototype> reagent)
    {
        var digested = GetDigestedReagentAmount(uid, reagent);

        if (!TryComp<BloodstreamComponent>(uid, out var blood))
            return digested;

        if (!_solution.ResolveSolution(uid, blood.BloodSolutionName, ref blood.BloodSolution, out var solution))
            return digested;

        return solution.GetTotalPrototypeQuantity(reagent) + digested;
    }

    public FixedPoint2 GetDigestedReagentAmount(EntityUid uid, ProtoId<ReagentPrototype> reagent)
    {
        if (!_proto.TryIndex(reagent, out var proto) || !DigestibleCureGroups.Contains(proto.Group))
            return FixedPoint2.Zero;

        if (!TryComp<BodyComponent>(uid, out var body))
            return FixedPoint2.Zero;

        if (!_body.TryGetOrgansWithComponent<StomachComponent>((uid, body), out var stomachs))
            return FixedPoint2.Zero;

        var total = FixedPoint2.Zero;
        foreach (var stomach in stomachs)
        {
            if (_solution.ResolveSolution(stomach.Owner, StomachSystem.DefaultSolutionName, ref stomach.Comp.Solution, out var stomachSolution))
                total += stomachSolution.GetTotalPrototypeQuantity(reagent);
        }

        return total;
    }

    public bool InStasis(EntityUid uid)
    {
        var ev = new GetMetabolicMultiplierEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Multiplier > 1f;
    }

    public bool HasDisease(EntityUid uid, ADTDisease disease)
    {
        if (!TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier))
            return false;

        return carrier.Diseases.Any(d => d.IsSame(disease));
    }

    public bool HasDisease(EntityUid uid, ProtoId<ADTDiseasePrototype> proto)
    {
        if (!TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier))
            return false;

        return carrier.Diseases.Any(d => !d.IsAdvance && d.Prototype == proto);
    }

    public IReadOnlyList<ADTDisease> GetDiseases(EntityUid uid)
    {
        if (!TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier))
            return Array.Empty<ADTDisease>();

        return carrier.Diseases;
    }

    public bool CanContractDisease(EntityUid uid, ADTDisease disease, ADTDiseasePrototype proto)
    {
        if (TryComp<ADTVirusImmuneComponent>(uid, out var immune))
        {
            if (immune.Absolute || !proto.IgnoreImmunity)
                return false;
        }

        var ev = new ADTDiseaseContractAttemptEvent(disease);
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled;
    }

    public bool CanContract(EntityUid uid, ADTDisease disease, ADTDiseaseSpread actType, bool protectionCheck, string? zone)
    {
        if (!_proto.TryIndex(disease.Prototype, out var proto))
            return false;

        if (proto.IsVirus && actType != ADTDiseaseSpread.None && (disease.Spread & actType) == 0)
            return false;

        if (!CanContractDisease(uid, disease, proto))
            return false;

        if (_mobState.IsDead(uid) && !proto.CanContractDead)
            return false;

        if (TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier))
        {
            if (carrier.Resistances.Contains(disease.GetId()))
                return false;

            if (carrier.Diseases.Any(d => d.IsSame(disease)))
                return false;
        }

        if (!IsInfectable(uid, proto))
            return false;

        if (protectionCheck && proto.IsVirus && CheckVirusProtection(uid, disease, actType, zone))
            return false;

        return true;
    }

    public bool IsInfectable(EntityUid uid, ADTDiseasePrototype proto)
    {
        if (proto.Infectable == null)
        {
            if (HasComp<HumanoidProfileComponent>(uid) || HasComp<ADTVirusSusceptibleComponent>(uid))
                return true;
        }
        else if (_whitelist.IsWhitelistPass(proto.Infectable, uid))
        {
            return true;
        }

        return proto.CarrierOnly != null && _whitelist.IsWhitelistPass(proto.CarrierOnly, uid);
    }

    public ADTDisease? Contract(
        EntityUid uid,
        ADTDisease disease,
        ADTDiseaseSpread actType = ADTDiseaseSpread.None,
        bool isCarrier = false,
        bool protectionCheck = false,
        string? zone = null)
    {
        if (!CanContract(uid, disease, actType, protectionCheck, zone))
            return null;

        var copy = disease.Copy();
        copy.Carrier = isCarrier;

        var carrier = EnsureComp<ADTDiseaseCarrierComponent>(uid);
        carrier.Diseases.Add(copy);
        if (carrier.NextUpdate == TimeSpan.Zero)
            carrier.NextUpdate = _timing.CurTime + carrier.UpdateInterval;

        if (copy.IsAdvance)
            RefreshAdvance(copy, updateProperties: false);

        if (_proto.TryIndex(copy.Prototype, out var contractedProto))
            BindMaster(uid, copy, contractedProto);

        UpdateBloodData(uid);
        RaiseDiseasesChanged(uid);
        return copy;
    }

    public ADTDisease? Contract(EntityUid uid, ProtoId<ADTDiseasePrototype> proto, bool isCarrier = false)
    {
        if (!_proto.TryIndex(proto, out var prototype))
            return null;

        return Contract(uid, ADTDisease.FromPrototype(prototype), isCarrier: isCarrier);
    }

    public void Cure(Entity<ADTDiseaseCarrierComponent?> ent, ADTDisease disease, bool needImmunity = true)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!ent.Comp.Diseases.Remove(disease))
            return;

        _proto.TryIndex(disease.Prototype, out var proto);
        if (proto != null && proto.CanImmunity && needImmunity)
            ent.Comp.Resistances.Add(disease.GetId());

        EndSymptoms(ent.Owner, disease);

        var ev = new ADTDiseaseCuredEvent(disease);
        RaiseLocalEvent(ent, ref ev);

        if (proto?.CuredMessage is { } msg)
            _popup.PopupEntity(Loc.GetString(msg), ent, ent);

        UpdateBloodData(ent);
        RaiseDiseasesChanged(ent);
    }

    public void CureAll(EntityUid uid, bool needImmunity = true)
    {
        if (!TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier))
            return;

        foreach (var disease in carrier.Diseases.ToList())
        {
            Cure((uid, carrier), disease, needImmunity);
        }
    }

    public void AddResistances(EntityUid uid, IEnumerable<string> ids)
    {
        var carrier = EnsureComp<ADTDiseaseCarrierComponent>(uid);
        carrier.Resistances.UnionWith(ids);
        UpdateBloodData(uid);
    }

    private bool TryMutate(Entity<ADTDiseaseCarrierComponent> ent, ADTDisease disease)
    {
        if (disease.MutationReagents.Any(r => !HasReagent(ent, r)))
            return false;

        if (disease.PossibleMutations.Count == 0)
            return false;

        var target = _random.Pick(disease.PossibleMutations);
        if (!_proto.TryIndex(target, out var targetProto))
            return false;

        ent.Comp.Diseases.Remove(disease);
        EndSymptoms(ent.Owner, disease);
        RaiseDiseasesChanged(ent);

        var mutated = targetProto.Symptoms.Count > 0
            ? CreatePresetAdvance(targetProto)
            : ADTDisease.FromPrototype(targetProto);

        Contract(ent, mutated);
        return true;
    }

    public void ForceStage(EntityUid uid, ADTDisease disease, int stage)
    {
        disease.Stage = Math.Clamp(stage, 1, disease.MaxStages);
        RaiseDiseasesChanged(uid);
    }

    public void RaiseDiseasesChanged(EntityUid uid)
    {
        UpdateDiseaseVisuals(uid);

        var ev = new ADTDiseasesChangedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    private void UpdateDiseaseVisuals(EntityUid uid)
    {
        ADTDiseaseSeverity? severity = null;

        if (TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier))
        {
            foreach (var disease in carrier.Diseases)
            {
                if ((disease.Visibility & ADTDiseaseVisibility.HiddenHud) != 0)
                    continue;

                if (!disease.Discovered && disease.Severity != ADTDiseaseSeverity.Positive)
                    continue;

                if (severity == null || disease.Severity > severity)
                    severity = disease.Severity;
            }
        }

        if (severity == null)
        {
            RemComp<ADTDiseaseVisualsComponent>(uid);
            return;
        }

        var visuals = EnsureComp<ADTDiseaseVisualsComponent>(uid);
        if (visuals.Severity == severity.Value)
            return;

        visuals.Severity = severity.Value;
        Dirty(uid, visuals);
    }

    public string GetDiseaseName(ADTDisease disease)
    {
        if (disease.IsAdvance)
        {
            var archived = GetArchived(disease.GetId());
            if (archived?.Name is { } archivedName)
                return archivedName;

            return disease.Name ?? Loc.GetString("adt-disease-unknown");
        }

        if (_proto.TryIndex(disease.Prototype, out var proto))
            return Loc.GetString(proto.Name);

        return Loc.GetString("adt-disease-unknown");
    }

    public string GetCureText(ADTDisease disease)
    {
        if (!disease.IsAdvance && _proto.TryIndex(disease.Prototype, out var proto) && proto.CureText is { } text)
            return Loc.GetString(text);

        if (disease.Cures.Count == 0)
            return Loc.GetString("adt-disease-cure-none");

        var names = disease.Cures
            .Select(c => _proto.TryIndex(c, out var r) ? r.LocalizedName : c.Id)
            .ToList();

        var separator = disease.NeedsAllCures
            ? Loc.GetString("adt-disease-cure-and")
            : Loc.GetString("adt-disease-cure-or");

        return string.Join(separator, names);
    }

    public string GetSpreadText(ADTDisease disease)
    {
        var parts = new List<string>();
        if (disease.Spread == ADTDiseaseSpread.None || disease.Spread == ADTDiseaseSpread.NonContagious)
            parts.Add(Loc.GetString("adt-disease-spread-none"));

        if ((disease.Spread & ADTDiseaseSpread.Bites) != 0)
            parts.Add(Loc.GetString("adt-disease-spread-bites"));

        if ((disease.Spread & ADTDiseaseSpread.Blood) != 0)
            parts.Add(Loc.GetString("adt-disease-spread-blood"));

        if ((disease.Spread & ADTDiseaseSpread.Contact) != 0)
            parts.Add(Loc.GetString("adt-disease-spread-contact"));

        if ((disease.Spread & ADTDiseaseSpread.Airborne) != 0)
            parts.Add(Loc.GetString("adt-disease-spread-airborne"));

        return string.Join(Loc.GetString("adt-disease-cure-and"), parts);
    }

    public List<ADTDiseaseInfo>? GetScannerDiseases(EntityUid uid, bool ignoreStealth = false)
    {
        if (!TryComp<ADTDiseaseCarrierComponent>(uid, out var carrier) || carrier.Diseases.Count == 0)
            return null;

        var result = new List<ADTDiseaseInfo>();
        foreach (var disease in carrier.Diseases)
        {
            if (!ignoreStealth && (disease.Visibility & ADTDiseaseVisibility.HiddenScanner) != 0)
                continue;

            if (!_proto.TryIndex(disease.Prototype, out var proto))
                continue;

            result.Add(new ADTDiseaseInfo
            {
                Name = GetDiseaseName(disease),
                Form = Loc.GetString(proto.Form),
                Cure = GetCureText(disease),
                Route = GetSpreadText(disease),
                Stage = disease.Stage,
                MaxStages = disease.MaxStages,
                Severity = disease.Severity,
            });
        }

        return result.Count > 0 ? result : null;
    }

    public void PopupDisease(EntityUid uid, string message, PopupType type = PopupType.SmallCaution)
    {
        _popup.PopupEntity(message, uid, uid, type);
    }
}
