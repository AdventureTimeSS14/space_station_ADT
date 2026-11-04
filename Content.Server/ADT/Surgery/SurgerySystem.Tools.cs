using Content.Shared.ADT.Body.OrganOrigin;
using Content.Shared.ADT.Surgery;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Prototypes;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Item;
using Content.Shared.Nutrition.Components;
using Content.Shared.Temperature;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    private const float StrapSurfaceModifier = 1.25f;
    private const float FloorSurfaceModifier = 1.5f;
    private const float MinSuccessChance = 0.05f;

    private static readonly ProtoId<SpeciesPrototype> ResomiSpecies = "Resomi";
    private static readonly HashSet<ProtoId<SpeciesPrototype>> ProsthesisForbiddenSpecies = new() { "SlimePerson", "Diona", ResomiSpecies };
    private static readonly ProtoId<SurgeryToolQualityPrototype> DrapingQuality = "Draping";
    private static readonly ProtoId<ItemSizePrototype> MaxCavityItemSize = "Small";

    private readonly record struct SurgeryToolMatch(float Speed, float Success, List<SurgeryStepEffect> FailureEffects, SoundSpecifier? Sound = null);

    private static readonly SurgeryToolMatch ExactMatch = new(1f, 1f, new());

    [Dependency] private SurgeryToolQualitySystem _toolQuality = default!;

    private bool TryMatchTool(EntityUid patient, SurgeryStepEntry step, EntityUid item, out SurgeryToolMatch match)
    {
        match = ExactMatch;

        if (step.Quality is { } qualityId)
            return TryMatchQuality(qualityId, item, out match);

        if (step.AnyItem)
            return IsStorableItem(item);

        return MatchesOrgan(patient, step, item);
    }

    private bool IsStorableItem(EntityUid item)
    {
        return TryComp<ItemComponent>(item, out var component)
               && !HasComp<SurgeryToolComponent>(item)
               && _item.GetItemSizeWeight(component.Size) <= _item.GetItemSizeWeight(MaxCavityItemSize);
    }

    private bool TryMatchQuality(ProtoId<SurgeryToolQualityPrototype> qualityId, EntityUid item, out SurgeryToolMatch match)
    {
        match = ExactMatch;

        if (TryComp<SurgeryToolComponent>(item, out var tool) && _toolQuality.GetToolQualities(item, tool).Contains(qualityId))
            return true;

        if (!PrototypeManager.TryIndex(qualityId, out var quality))
            return false;

        foreach (var entry in quality.Entries)
        {
            if (!_toolQuality.EntryMatches(entry, item))
                continue;

            match = new SurgeryToolMatch(entry.Speed, entry.Success, entry.FailureEffects, entry.Sound);
            return true;
        }

        return false;
    }

    private bool MatchesOrgan(EntityUid patient, SurgeryStepEntry step, EntityUid item)
    {
        if (step.Organ is not { } category)
            return false;

        if (TryComp<OrganComponent>(item, out var organ) && organ.Category == category)
            return IsPartAccepted(patient, step, item);

        return _detachableOrgan.TryGetDetachedOrgan(item, out var detached)
               && TryComp<OrganComponent>(detached, out var detachedOrgan)
               && detachedOrgan.Category == category
               && IsPartAccepted(patient, step, detached);
    }

    private bool IsPartAccepted(EntityUid patient, SurgeryStepEntry step, EntityUid part)
    {
        return (!step.Robotic || _roboticPart.IsRobotic(part)) && IsLimbCompatible(patient, part);
    }

    private bool IsLimbCompatible(EntityUid patient, EntityUid limb)
    {
        if (!HasComp<DetachableOrganComponent>(limb))
            return true;

        TryComp<HumanoidProfileComponent>(patient, out var profile);

        if (_roboticPart.IsRobotic(limb) && profile != null && ProsthesisForbiddenSpecies.Contains(profile.Species))
            return false;

        if (!TryComp<OrganOriginComponent>(limb, out var origin) || origin.Species != ResomiSpecies)
            return true;

        return profile?.Species == ResomiSpecies;
    }

    private TimeSpan GetStepDuration(EntityUid patient, SurgeryStepEntry step, SurgeryToolMatch match, EntityUid item)
    {
        var toolSpeed = CompOrNull<SurgeryToolComponent>(item)?.Speed ?? 1f;
        return step.Duration * (match.Speed * toolSpeed * GetSurfaceModifier(patient));
    }

    private SoundSpecifier? GetStepSound(SurgeryStepEntry step, SurgeryToolMatch match, EntityUid item)
    {
        return CompOrNull<SurgeryToolComponent>(item)?.Sound ?? match.Sound ?? step.Sound;
    }

    private float GetSurfaceModifier(EntityUid patient)
    {
        if (!TryComp<BuckleComponent>(patient, out var buckle) || buckle.BuckledTo is not { } strap)
            return FloorSurfaceModifier;

        if (TryComp<OperatingTableComponent>(strap, out var table))
            return table.Modifier;

        return StrapSurfaceModifier;
    }

    private bool IsSurgeonTrained(EntityUid? surgeon)
    {
        return HasComp<SurgerySkillComponent>(surgeon);
    }

    private float GetSuccessChance(EntityUid patient, EntityUid? surgeon, SurgeryStepEntry step, SurgeryToolMatch match, bool robotic)
    {
        var baseChance = IsSurgeonTrained(surgeon) ? 1f : step.SuccessChance;
        var anesthesia = robotic ? 1f : GetAnesthesiaMultiplier(patient);
        return Math.Clamp(baseChance * match.Success * anesthesia, MinSuccessChance, 1f);
    }
}
