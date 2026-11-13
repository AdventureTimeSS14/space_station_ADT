using Content.Shared.ADT.Body.Limbs;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Prosthesis;

public sealed class RoboticPartSystem : EntitySystem
{
    private static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";

    private static readonly string[] ProsthesisMarkings =
    {
        "Cyberlimb",
        "Augments",
        "Prosthetic",
        "ReptilianImplantCybersun",
    };

    [Dependency] private BodySystem _body = default!;

    public bool IsRobotic(EntityUid organ)
    {
        if (HasComp<RoboticPartComponent>(organ))
            return true;

        if (TryComp<OrganHealthComponent>(organ, out var health) && health.Kind == OrganKind.Robotic)
            return true;

        return HasComp<LimbOrganComponent>(organ) && HasProsthesisMarking(organ);
    }

    public OrganKind GetKind(EntityUid body, ProtoId<OrganCategoryPrototype>? part)
    {
        if (part is { } category && TryGetOrgan(body, category, out var organ))
            return IsRobotic(organ) ? OrganKind.Robotic : OrganKind.Organic;

        return TryGetOrgan(body, Torso, out var torso) && IsRobotic(torso)
            ? OrganKind.Robotic
            : OrganKind.Organic;
    }

    public static bool IsProsthesisMarking(string markingId)
    {
        foreach (var name in ProsthesisMarkings)
        {
            if (markingId.Contains(name))
                return true;
        }

        return false;
    }

    private bool TryGetOrgan(EntityUid body, ProtoId<OrganCategoryPrototype> category, out EntityUid organ)
    {
        foreach (var candidate in _body.EnumerateOrgans<OrganComponent>(body))
        {
            if (candidate.Comp1.Category != category)
                continue;

            organ = candidate.Owner;
            return true;
        }

        organ = default;
        return false;
    }

    private bool HasProsthesisMarking(EntityUid organ)
    {
        if (!TryComp<VisualOrganMarkingsComponent>(organ, out var visual))
            return false;

        foreach (var (_, markings) in visual.Markings)
        {
            foreach (var marking in markings)
            {
                if (IsProsthesisMarking(marking.MarkingId))
                    return true;
            }
        }

        return false;
    }
}
