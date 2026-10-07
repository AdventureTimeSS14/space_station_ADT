using Content.Shared.Antag;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Player;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    private bool IsProfileSpeciesValid(ICommonSession player, AntagSpecifierPrototype def)
    {
        if (def.Blacklist?.Components is not { Length: > 0 } blacklist)
            return true;

        if (!_pref.TryGetCachedPreferences(player.UserId, out var prefs) ||
            prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
            return true;

        if (!ProtoMan.TryIndex<SpeciesPrototype>(profile.Species, out var species) ||
            !ProtoMan.TryIndex(species.Prototype, out var entity))
            return true;

        foreach (var component in blacklist)
        {
            if (entity.Components.ContainsKey(component))
                return false;
        }

        return true;
    }
}
