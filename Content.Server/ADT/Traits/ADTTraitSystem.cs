using System.Linq;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Traits.Effects;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Traits;

public sealed class ADTTraitSystem : EntitySystem
{
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private ILogManager _log = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    private int _maxTraitCount;
    private int _maxTraitPoints;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        Subs.CVar(_config, SimpleStationCCVars.MaxTraitCount, value => _maxTraitCount = value, true);
        Subs.CVar(_config, SimpleStationCCVars.MaxTraitPoints, value => _maxTraitPoints = value, true);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId == null ||
            !ProtoMan.TryIndex<JobPrototype>(args.JobId, out var jobProto) ||
            !jobProto.ApplyTraits)
            return;

        ApplyTraits(args.Mob, args.Profile, args.Player, args.JobId);
    }

    public void ApplyTraits(EntityUid mob, HumanoidCharacterProfile profile, ICommonSession session, ProtoId<JobPrototype>? jobId = null)
    {
        ApplyValidTraits(mob, ValidateTraits(profile.TraitPreferences, session, profile, jobId));
    }

    public void ApplyTraits(EntityUid mob, HumanoidCharacterProfile profile, ProtoId<JobPrototype>? jobId = null)
    {
        ApplyValidTraits(mob, ValidateTraits(profile.TraitPreferences, null, profile, jobId));
    }

    private void ApplyValidTraits(EntityUid mob, HashSet<ProtoId<TraitPrototype>> validTraits)
    {
        foreach (var traitId in validTraits)
        {
            if (!ProtoMan.TryIndex(traitId, out var trait))
                continue;

            ApplyTrait(mob, trait);
        }
    }

    private HashSet<ProtoId<TraitPrototype>> ValidateTraits(
        IReadOnlySet<ProtoId<TraitPrototype>> selectedTraits,
        ICommonSession? session,
        HumanoidCharacterProfile? profile,
        ProtoId<JobPrototype>? jobId = null)
    {
        var validTraits = new HashSet<ProtoId<TraitPrototype>>();
        var totalPoints = 0;
        var traitCount = 0;
        var categoryTraitCounts = new Dictionary<ProtoId<TraitCategoryPrototype>, int>();
        var categoryPointTotals = new Dictionary<ProtoId<TraitCategoryPrototype>, int>();

        foreach (var traitId in selectedTraits)
        {
            if (!ProtoMan.TryIndex(traitId, out var trait))
            {
                Log.Warning($"Unknown trait ID in player preferences: {traitId}");
                continue;
            }

            if (traitCount >= _maxTraitCount)
            {
                Log.Warning($"Trait {traitId} rejected: global trait count limit ({_maxTraitCount}) exceeded");
                continue;
            }

            if (totalPoints + trait.Cost > _maxTraitPoints)
            {
                Log.Warning($"Trait {traitId} rejected: global points limit ({_maxTraitPoints}) would be exceeded");
                continue;
            }

            if (!ValidateCategoryLimits(trait, categoryTraitCounts, categoryPointTotals))
            {
                Log.Warning($"Trait {traitId} rejected: category limits exceeded");
                continue;
            }

            if (HasConflict(trait, validTraits))
                continue;

            var playTimes = new Dictionary<string, TimeSpan>();
            if (!JobRequirements.TryRequirementsMet(trait.Requirements, playTimes, out _, EntityManager, ProtoMan, profile))
            {
                Log.Warning($"Trait {traitId} rejected: requirements not met");
                continue;
            }

            if (profile != null)
            {
                if (trait.SpeciesWhitelist.Count > 0 && !trait.SpeciesWhitelist.Contains(profile.Species))
                {
                    Log.Warning($"Trait {traitId} rejected: species {profile.Species} not in whitelist");
                    continue;
                }

                if (trait.SpeciesBlacklist.Contains(profile.Species))
                {
                    Log.Warning($"Trait {traitId} rejected: species {profile.Species} in blacklist");
                    continue;
                }
            }

            if (jobId.HasValue && !IsJobAllowed(trait, jobId.Value))
                continue;

            validTraits.Add(traitId);
            totalPoints += trait.Cost;
            traitCount++;

            var category = trait.ADTCategory;
            categoryTraitCounts[category] = categoryTraitCounts.GetValueOrDefault(category) + 1;
            categoryPointTotals[category] = categoryPointTotals.GetValueOrDefault(category) + trait.Cost;
        }

        return validTraits;
    }

    private bool HasConflict(TraitPrototype trait, HashSet<ProtoId<TraitPrototype>> validTraits)
    {
        foreach (var validTraitId in validTraits)
        {
            if (trait.Conflicts.Contains(validTraitId))
            {
                Log.Warning($"Trait {trait.ID} rejected: conflicts with {validTraitId}");
                return true;
            }

            if (ProtoMan.TryIndex(validTraitId, out var validTrait) &&
                validTrait.Conflicts.Contains(trait.ID))
            {
                Log.Warning($"Trait {trait.ID} rejected: {validTraitId} conflicts with it");
                return true;
            }
        }

        return false;
    }

    private bool IsJobAllowed(TraitPrototype trait, ProtoId<JobPrototype> jobId)
    {
        if (trait.JobWhitelist.Count > 0 && !trait.JobWhitelist.Contains(jobId))
        {
            Log.Warning($"Trait {trait.ID} rejected: job {jobId} not in whitelist");
            return false;
        }

        if (trait.JobBlacklist.Contains(jobId))
        {
            Log.Warning($"Trait {trait.ID} rejected: job {jobId} in blacklist");
            return false;
        }

        if (trait.DepartmentWhitelist.Count == 0 && trait.DepartmentBlacklist.Count == 0)
            return true;

        var departments = new List<ProtoId<DepartmentPrototype>>();
        foreach (var department in ProtoMan.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (department.Roles.Contains(jobId))
                departments.Add(department.ID);
        }

        if (trait.DepartmentWhitelist.Count > 0 && !departments.Any(trait.DepartmentWhitelist.Contains))
        {
            Log.Warning($"Trait {trait.ID} rejected: job {jobId} departments not in whitelist");
            return false;
        }

        if (departments.Any(trait.DepartmentBlacklist.Contains))
        {
            Log.Warning($"Trait {trait.ID} rejected: job {jobId} has blacklisted department");
            return false;
        }

        return true;
    }

    private bool ValidateCategoryLimits(
        TraitPrototype trait,
        Dictionary<ProtoId<TraitCategoryPrototype>, int> categoryTraitCounts,
        Dictionary<ProtoId<TraitCategoryPrototype>, int> categoryPointTotals)
    {
        if (!ProtoMan.TryIndex(trait.ADTCategory, out var category))
            return true;

        var currentCount = categoryTraitCounts.GetValueOrDefault(category.ID);
        var currentPoints = categoryPointTotals.GetValueOrDefault(category.ID);

        if (category.MaxTraits.HasValue && currentCount >= category.MaxTraits.Value)
            return false;

        if (category.MaxTraitPoints.HasValue && currentPoints + trait.Cost > category.MaxTraitPoints.Value)
            return false;

        return true;
    }

    private void ApplyTrait(EntityUid player, TraitPrototype trait)
    {
        if (_whitelistSystem.IsWhitelistFail(trait.Whitelist, player) ||
            _whitelistSystem.IsWhitelistPass(trait.Blacklist, player))
            return;

        var transform = Transform(player);

        var effectCtx = new TraitEffectContext
        {
            Player = player,
            EntMan = EntityManager,
            Proto = ProtoMan,
            CompFactory = _factory,
            LogMan = _log,
            Transform = transform,
        };

        foreach (var effect in trait.Effects)
        {
            try
            {
                if (effect is SpawnItemInHandEffect spawnEffect)
                    ApplySpawnItemEffect(player, spawnEffect, transform);
                else if (effect is SpawnItemInBackpackEffect backpackEffect)
                    ApplySpawnItemBackpackEffect(player, backpackEffect, transform);
                else
                    effect.Apply(effectCtx);
            }
            catch (Exception e)
            {
                Log.Error($"Error applying effect {effect.GetType().Name} for trait {trait.ID}: {e}");
            }
        }

        foreach (var special in trait.Specials)
        {
            special.AfterEquip(player);
        }

        if (trait.Effects.Count != 0)
            return;

#pragma warning disable CS0618
        if (trait.Components.Count > 0)
            EntityManager.AddComponents(player, trait.Components, trait.RewriteComponents);
#pragma warning restore CS0618

        if (trait.TraitGear != null && TryComp(player, out HandsComponent? handsComponent))
        {
            var item = Spawn(trait.TraitGear, transform.Coordinates);
            _hands.TryPickup(player, item, checkActionBlocker: false, handsComp: handsComponent);
        }
    }

    private void ApplySpawnItemEffect(EntityUid player, SpawnItemInHandEffect effect, TransformComponent transform)
    {
        if (!TryComp<HandsComponent>(player, out var hands))
        {
            Log.Warning("Cannot spawn trait item: player has no hands component");
            return;
        }

        var item = Spawn(effect.Item, transform.Coordinates);

        if (!_hands.TryPickup(player, item, checkActionBlocker: false, handsComp: hands))
            Log.Debug($"Could not pick up trait item {effect.Item}, leaving at feet");
    }

    private void ApplySpawnItemBackpackEffect(EntityUid player, SpawnItemInBackpackEffect effect, TransformComponent transform)
    {
        var item = Spawn(effect.Item, transform.Coordinates);

        if (_inventory.TryGetSlotEntity(player, "back", out var backpack) &&
            _container.TryGetContainer(backpack.Value, "storagebase", out var container) &&
            _container.Insert(item, container))
        {
            return;
        }

        Log.Debug($"Could not put trait item {effect.Item} into backpack, leaving at feet");
    }
}
