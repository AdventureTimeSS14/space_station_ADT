using Content.Shared.ADT.Language;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Effect that removes all player-chosen languages but keeps the species default languages
/// and any additional languages specified in the PreserveLanguages list.
/// This is used for traits like "Sheltered" that should remove learned languages while
/// preserving the innate languages of the species and any other specified languages.
/// </summary>
public sealed partial class RemoveLanguagesEffect : BaseTraitEffect
{
    [DataField]
    public List<ProtoId<LanguagePrototype>> PreserveLanguages = new();

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent<LanguageSpeakerComponent>(ctx.Player, out var language))
            return;

        SpeciesPrototype? speciesPrototype = null;

        if (ctx.EntMan.TryGetComponent<HumanoidProfileComponent>(ctx.Player, out var humanoid))
        {
            if (ctx.Proto.TryIndex<SpeciesPrototype>(humanoid.Species, out var species))
            {
                speciesPrototype = species;
            }
        }

        HashSet<string> languagesToKeep = new();

        if (speciesPrototype != null)
        {
            foreach (var langProtoId in speciesPrototype.DefaultLanguages)
            {
                // Skip GalacticCommon - it's a universal language and should be removed
                if (langProtoId == "GalacticCommon")
                    continue;

                languagesToKeep.Add(langProtoId);
            }

            foreach (var langProtoId in speciesPrototype.ForceLanguages)
            {
                if (langProtoId == "GalacticCommon")
                    continue;

                languagesToKeep.Add(langProtoId);
            }
        }

        foreach (var langProtoId in PreserveLanguages)
        {
            languagesToKeep.Add(langProtoId);
        }

        var languagesToRemove = language.Languages.Keys.Where(k => !languagesToKeep.Contains(k)).ToList();
        foreach (var langId in languagesToRemove)
        {
            language.Languages.Remove(langId);
        }
    }
}
