using System.Linq;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.Drake.Loot;
using Content.Shared.ADT.Language;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Drake.Loot;

public sealed partial class ADTDraconidTransformationSystem : EntityEffectSystem<HumanoidProfileComponent, ADTDraconidTransformation>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> entity, ref EntityEffectEvent<ADTDraconidTransformation> args)
    {
        var effect = args.Effect;
        var old = entity.Owner;

        var oldSpecies = entity.Comp.Species;
        var sex = entity.Comp.Sex;
        var gender = entity.Comp.Gender;

        _visualBody.TryGatherMarkingsData(old, null, out _, out _, out var oldMarkings);

        var keptDamage = GetKeptDamage(old, effect.HealedGroups);

        HashSet<ProtoId<NpcFactionPrototype>>? factions = null;
        if (TryComp<NpcFactionMemberComponent>(old, out var oldFaction))
            factions = new HashSet<ProtoId<NpcFactionPrototype>>(oldFaction.Factions);

        float? hunger = null;
        if (TryComp<HungerComponent>(old, out var oldHunger))
            hunger = _hunger.GetHunger(oldHunger);

        float? thirst = null;
        if (TryComp<ThirstComponent>(old, out var oldThirst))
            thirst = oldThirst.CurrentThirst;

        var polymorph = HasComp<ADTTribeMemberComponent>(old) ? effect.TribePolymorph : effect.Polymorph;
        if (_polymorph.PolymorphEntity(old, polymorph) is not { } body)
            return;

        RemCompDeferred<PolymorphedEntityComponent>(body);

        _humanoid.SetSex((body, null), sex);
        _humanoid.SetGender((body, null), gender);

        ApplyAppearance(body, effect, sex, oldMarkings);

        if (keptDamage != null && !keptDamage.Empty)
            _damageable.TryChangeDamage(body, keptDamage, true, false);

        if (factions != null)
        {
            _faction.ClearFactions(body, false);
            _faction.AddFactions(body, factions);
        }

        if (hunger is { } hungerValue && TryComp<HungerComponent>(body, out var newHunger))
            _hunger.SetHunger(body, hungerValue, newHunger);

        if (thirst is { } thirstValue && TryComp<ThirstComponent>(body, out var newThirst))
            _thirst.SetThirst(body, newThirst, thirstValue);

        ReplaceSpeciesLanguages(body, oldSpecies);
    }

    private DamageSpecifier? GetKeptDamage(EntityUid uid, List<ProtoId<DamageGroupPrototype>> healedGroups)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return null;

        var kept = new DamageSpecifier(damageable.Damage);

        foreach (var groupId in healedGroups)
        {
            if (!_prototype.TryIndex(groupId, out var group))
                continue;

            foreach (var type in group.DamageTypes)
            {
                kept.DamageDict.Remove(type);
            }
        }

        return kept;
    }

    private void ApplyAppearance(
        EntityUid body,
        ADTDraconidTransformation effect,
        Sex sex,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>? oldMarkings)
    {
        _visualBody.ApplyProfile(body,
            new OrganProfileData
            {
                Sex = sex,
                SkinColor = effect.SkinColor,
                EyeColor = effect.EyeColor,
            });

        if (!_visualBody.TryGatherMarkingsData(body, null, out _, out var markingData, out _))
            return;

        var hornsColor = effect.HornsColor;
        var markings = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        foreach (var (category, data) in markingData)
        {
            var layers = new Dictionary<HumanoidVisualLayers, List<Marking>>();
            Dictionary<HumanoidVisualLayers, List<Marking>>? oldLayers = null;
            oldMarkings?.TryGetValue(category, out oldLayers);

            foreach (var layer in data.Layers)
            {
                if (oldLayers != null && oldLayers.TryGetValue(layer, out var oldLayer))
                    layers[layer] = oldLayer.ToList();
                else
                    layers[layer] = new List<Marking>();
            }

            markings[category] = layers;
        }

        if (oldMarkings != null
            && oldMarkings.TryGetValue(effect.HornsCategory, out var oldHead)
            && oldHead.TryGetValue(effect.HornsLayer, out var oldHorns)
            && oldHorns.Count > 0
            && oldHorns[0].MarkingColors.Count > 0)
        {
            hornsColor = oldHorns[0].MarkingColors[0];
        }

        if (markings.TryGetValue(effect.HornsCategory, out var head) && _prototype.TryIndex(effect.HornsMarking, out var hornsProto))
        {
            head[effect.HornsLayer] = new List<Marking>
            {
                new(effect.HornsMarking, Enumerable.Repeat(hornsColor, hornsProto.Sprites.Count)),
            };
        }

        _visualBody.ApplyMarkings(body, markings);
    }

    private void ReplaceSpeciesLanguages(EntityUid body, ProtoId<SpeciesPrototype> oldSpecies)
    {
        if (!TryComp<LanguageSpeakerComponent>(body, out var speaker) || !TryComp<HumanoidProfileComponent>(body, out var humanoid))
            return;

        if (!_prototype.TryIndex(humanoid.Species, out var newSpecies))
            return;

        if (_prototype.TryIndex(oldSpecies, out var oldSpeciesProto))
        {
            foreach (var language in oldSpeciesProto.DefaultLanguages)
            {
                if (!newSpecies.DefaultLanguages.Contains(language))
                    _language.RemoveLanguage(body, language, speaker);
            }
        }

        foreach (var language in newSpecies.DefaultLanguages)
        {
            _language.AddSpokenLanguage(body, language, LanguageKnowledge.Speak, speaker);
        }
    }
}
