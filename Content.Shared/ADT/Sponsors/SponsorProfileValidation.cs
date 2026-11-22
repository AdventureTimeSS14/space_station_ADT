using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.ADT.TTS;
using Content.Shared.Traits;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Sponsors;

public static class SponsorProfileValidation
{
    private static bool TryGetManager(IDependencyCollection collection, out ISharedSponsorManager manager)
    {
        manager = default!;

        if (!collection.TryResolveType(out ISharedSponsorManager? resolved))
            return false;

        if (collection.TryResolveType(out IConfigurationManager? cfg)
            && cfg.IsCVarRegistered(SponsorCVars.EnforceProfile.Name)
            && !cfg.GetCVar(SponsorCVars.EnforceProfile))
        {
            return false;
        }

        manager = resolved;
        return true;
    }

    public static bool IsSpeciesAllowed(
        ICommonSession? session,
        IDependencyCollection collection,
        SpeciesPrototype species)
    {
        if (!species.SponsorOnly)
            return true;

        if (!TryGetManager(collection, out var manager))
            return true;

        return manager.IsSpeciesAllowed(session, species.ID);
    }

    public static bool IsTraitAllowed(
        ICommonSession? session,
        IDependencyCollection collection,
        TraitPrototype trait)
    {
        if (!trait.SponsorOnly)
            return true;

        if (!TryGetManager(collection, out var manager))
            return true;

        return manager.IsTraitAllowed(session, trait.ID);
    }

    public static bool IsLoadoutAllowed(
        ICommonSession? session,
        IDependencyCollection collection,
        LoadoutPrototype loadout)
    {
        if (!loadout.SponsorOnly)
            return true;

        if (!TryGetManager(collection, out var manager))
            return true;

        return manager.IsLoadoutAllowed(session, loadout.ID);
    }

    public static bool IsMarkingAllowed(
        ICommonSession? session,
        IDependencyCollection collection,
        string markingId)
    {
        var protoManager = collection.Resolve<IPrototypeManager>();

        if (!protoManager.TryIndex<MarkingPrototype>(markingId, out var marking) || !marking.SponsorOnly)
            return true;

        if (!TryGetManager(collection, out var manager))
            return true;

        return manager.IsMarkingAllowed(session, markingId);
    }

    public static bool IsTtsVoiceAllowed(
        ICommonSession? session,
        IDependencyCollection collection,
        TTSVoicePrototype voice)
    {
        if (!voice.SponsorOnly)
            return true;

        if (!TryGetManager(collection, out var manager))
            return true;

        return manager.IsTtsVoiceAllowed(session, voice.ID);
    }

    public static void StripMarkings(
        HumanoidCharacterAppearance appearance,
        ICommonSession? session,
        IDependencyCollection collection)
    {
        if (!TryGetManager(collection, out var manager))
            return;

        var protoManager = collection.Resolve<IPrototypeManager>();

        foreach (var (_, layers) in appearance.Markings)
        {
            foreach (var (_, markings) in layers)
            {
                for (var i = markings.Count - 1; i >= 0; i--)
                {
                    var id = markings[i].MarkingId;

                    if (!protoManager.TryIndex<MarkingPrototype>(id, out var proto) || !proto.SponsorOnly)
                        continue;

                    if (manager.IsMarkingAllowed(session, id.Id))
                        continue;

                    markings.RemoveAt(i);
                }
            }
        }
    }

    public static List<ProtoId<TraitPrototype>> FilterTraits(
        IEnumerable<ProtoId<TraitPrototype>> traits,
        ICommonSession? session,
        IDependencyCollection collection)
    {
        var protoManager = collection.Resolve<IPrototypeManager>();

        return traits
            .Where(id => !protoManager.TryIndex(id, out var proto) || IsTraitAllowed(session, collection, proto))
            .ToList();
    }
}
