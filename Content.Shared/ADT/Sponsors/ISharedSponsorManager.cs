using System.Diagnostics.CodeAnalysis;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Sponsors;

public interface ISharedSponsorManager
{
    SponsorData GetData(ICommonSession? session);

    bool TryGetData(ICommonSession? session, [NotNullWhen(true)] out SponsorData? data);

    bool IsLoadoutAllowed(ICommonSession? session, string loadoutId);

    bool IsMarkingAllowed(ICommonSession? session, string markingId);

    bool IsSpeciesAllowed(ICommonSession? session, string speciesId);

    bool IsTraitAllowed(ICommonSession? session, string traitId);

    bool IsTtsVoiceAllowed(ICommonSession? session, string voiceId);

    bool IsJobTimeBypassed(ICommonSession? session, ProtoId<JobPrototype> job);

    bool IsAntagTimeBypassed(ICommonSession? session);
}
