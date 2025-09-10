using Content.Server.Ghost.Roles.Components;
using Content.Server.Speech.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Components; // ADT-tweak
using Content.Shared.NPC.Systems; // ADT-tweak
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Language;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class MakeSentient : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;

        // Let affected entities speak normally to make this effect different from, say, the "random sentience" event
        // This also works on entities that already have a mind
        // We call this before the mind check to allow things like player-controlled mice to be able to benefit from the effect
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);
        entityManager.RemoveComponent<MonkeyAccentComponent>(uid);

        // ADT Languages start
        var lang = entityManager.EnsureComponent<LanguageSpeakerComponent>(uid);
        if (!lang.Languages.ContainsKey("GalacticCommon"))
            lang.Languages.Add("GalacticCommon", LanguageKnowledge.Speak);
        else
            lang.Languages["GalacticCommon"] = LanguageKnowledge.Speak;
        // ADT Languages end

        // Stops from adding a ghost role to things like people who already have a mind
        if (entityManager.TryGetComponent<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
        {
            return;
        }

        // Don't add a ghost role to things that already have ghost roles
        if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
        {
            return;
        }

        MakeFriendlyToStation(uid, entityManager); // ADT-tweak

        ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
        entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }

    /// <summary>
    /// ADT-tweak start
    /// Делает моба дружественным к станции путем смены фракции
    /// </summary>
    private void MakeFriendlyToStation(EntityUid uid, IEntityManager entityManager)
    {
        var factionSystem = entityManager.System<NpcFactionSystem>();

        if (!entityManager.TryGetComponent<NpcFactionMemberComponent>(uid, out var factionComp))
        {
            factionSystem.AddFaction(uid, "PetsNT");
            return;
        }

        var hostileFactions = new[]
        {
            "SimpleHostile", "Xeno", "Dragon", "Syndicate",
            "AllHostile", "Wizard", "ADTSpaceMobs"
        };

        bool wasHostile = false;

        foreach (var hostileFaction in hostileFactions)
        {
            if (factionSystem.IsMember(uid, hostileFaction))
            {
                factionSystem.RemoveFaction(uid, hostileFaction, false);
                wasHostile = true;
            }
        }

        if (wasHostile)
        {
            factionSystem.AddFaction(uid, "PetsNT", true);
        }
        else
        {
            factionSystem.AddFaction(uid, "SimpleNeutral", true);
        }
    }
    // ADT-tweak end
}