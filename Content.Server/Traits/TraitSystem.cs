// Система полностью переписана под ADT, под новые трейты

using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Traits.Effects;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

/// <summary>
/// Server system that validates and applies traits to players on spawn.
/// </summary>
public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

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
        // Check if player's job allows traits
        if (args.JobId == null ||
<<<<<<< HEAD
            !_prototype.TryIndex<JobPrototype>(args.JobId, out var jobProto) ||
            !jobProto.ApplyTraits)
            return;

        // Validate and collect valid traits
        var validTraits = ValidateTraits(args.Mob, args.Profile.TraitPreferences, args.Player, args.Profile, args.JobId);

        // Apply valid traits
        foreach (var traitId in validTraits)
=======
            !_prototypeManager.Resolve<JobPrototype>(args.JobId, out var protoJob) ||
            !protoJob.ApplyTraits)
>>>>>>> upstreamwiz/master
        {
            if (!_prototype.TryIndex(traitId, out var trait))
                continue;

            ApplyTrait(args.Mob, trait);
        }
    }

    /// <summary>
    /// Applies traits to an entity.
    /// </summary>
    public void ApplyTraits(EntityUid mob, HumanoidCharacterProfile profile, ICommonSession session, ProtoId<JobPrototype>? jobId = null)
    {
        var validTraits = ValidateTraits(mob, profile.TraitPreferences, session, profile, jobId);

        foreach (var traitId in validTraits)
        {
            if (!_prototype.TryIndex(traitId, out var trait))
                continue;

            ApplyTrait(mob, trait);
        }
    }

    /// <summary>
    /// Validates a set of trait selections against all rules and returns the valid subset.
    /// </summary>
    private HashSet<ProtoId<TraitPrototype>> ValidateTraits(
        EntityUid player,
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
            if (!_prototype.TryIndex(traitId, out var trait))
            {
                Log.Warning($"Unknown trait ID in player preferences: {traitId}");
                continue;
            }

            // Check global trait count limit
            if (traitCount >= _maxTraitCount)
            {
                Log.Warning($"Trait {traitId} rejected: global trait count limit ({_maxTraitCount}) exceeded");
                continue;
            }

            // Check global points limit
            var newTotal = totalPoints + trait.Cost;
            if (newTotal > _maxTraitPoints)
            {
                Log.Warning(
                    $"Trait {traitId} rejected: global points limit ({_maxTraitPoints}) would be exceeded");
                continue;
            }

            // Check category limits
            if (!ValidateCategoryLimits(trait, categoryTraitCounts, categoryPointTotals))
            {
                Log.Warning($"Trait {traitId} rejected: category limits exceeded");
                continue;
            }

            // Check conflicts with already selected traits
            var hasConflict = false;
            foreach (var validTraitId in validTraits)
            {
                // Check if current trait conflicts with valid trait
                if (trait.Conflicts.Contains(validTraitId))
                {
                    Log.Warning($"Trait {traitId} rejected: conflicts with {validTraitId}");
                    hasConflict = true;
                    break;
                }

                // Check if valid trait conflicts with current trait
                if (_prototype.TryIndex(validTraitId, out var validTrait) &&
                    validTrait.Conflicts.Contains(traitId))
                {
                    Log.Warning($"Trait {traitId} rejected: {validTraitId} conflicts with it");
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict)
                continue;

            // Check requirements
            var playTimes = session != null ? new Dictionary<string, TimeSpan>() : new Dictionary<string, TimeSpan>();
            if (!JobRequirements.TryRequirementsMet(trait.Requirements, playTimes, out _, EntityManager, _prototype, profile))
            {
                Log.Warning($"Trait {traitId} rejected: requirements not met");
                continue;
            }

            // Check species whitelist/blacklist (ADT legacy support)
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

            if (jobId.HasValue)
            {
                if (trait.JobWhitelist.Count > 0 && !trait.JobWhitelist.Contains(jobId.Value))
                {
                    Log.Warning($"Trait {traitId} rejected: job {jobId} not in whitelist");
                    continue;
                }

                if (trait.JobBlacklist.Contains(jobId.Value))
                {
                    Log.Warning($"Trait {traitId} rejected: job {jobId} in blacklist");
                    continue;
                }

                if (trait.DepartmentWhitelist.Count > 0 || trait.DepartmentBlacklist.Count > 0)
                {
                    var jobProto = _prototype.Index(jobId.Value);
                    var allDepartments = new List<ProtoId<DepartmentPrototype>>();

                    foreach (var deptProto in _prototype.EnumeratePrototypes<DepartmentPrototype>())
                    {
                        if (deptProto.Roles.Contains(jobId.Value))
                            allDepartments.Add(deptProto.ID);
                    }

                    if (trait.DepartmentWhitelist.Count > 0)
                    {
                        var hasWhitelistedDept = false;
                        foreach (var deptId in allDepartments)
                        {
                            if (trait.DepartmentWhitelist.Contains(deptId))
                            {
                                hasWhitelistedDept = true;
                                break;
                            }
                        }

                        if (!hasWhitelistedDept)
                        {
                            Log.Warning($"Trait {traitId} rejected: job {jobId} departments not in whitelist");
                            continue;
                        }
                    }

                    if (trait.DepartmentBlacklist.Count > 0)
                    {
                        var hasBlacklistedDept = false;
                        foreach (var deptId in allDepartments)
                        {
                            if (trait.DepartmentBlacklist.Contains(deptId))
                            {
                                hasBlacklistedDept = true;
                                break;
                            }
                        }

                        if (hasBlacklistedDept)
                        {
                            Log.Warning($"Trait {traitId} rejected: job {jobId} has blacklisted department");
                            continue;
                        }
                    }
                }
            }

            // Trait is valid, add it
            validTraits.Add(traitId);
            totalPoints += trait.Cost;
            traitCount++;

            // Update category tracking
            categoryTraitCounts.TryGetValue(trait.Category, out var catCount);
            categoryTraitCounts[trait.Category] = catCount + 1;

            categoryPointTotals.TryGetValue(trait.Category, out var catPoints);
            categoryPointTotals[trait.Category] = catPoints + trait.Cost;
        }

        return validTraits;
    }

    /// <summary>
    /// Validates that adding a trait wouldn't exceed category-specific limits.
    /// </summary>
    private bool ValidateCategoryLimits(
        TraitPrototype trait,
        Dictionary<ProtoId<TraitCategoryPrototype>, int> categoryTraitCounts,
        Dictionary<ProtoId<TraitCategoryPrototype>, int> categoryPointTotals)
    {
        if (!_prototype.TryIndex(trait.Category, out var category))
            return true; // Unknown category, allow it

        categoryTraitCounts.TryGetValue(trait.Category, out var currentCount);
        categoryPointTotals.TryGetValue(trait.Category, out var currentPoints);

        // Check category trait count limit
        if (category.MaxTraits.HasValue && currentCount >= category.MaxTraits.Value)
            return false;

        // Check category points limit
        if (category.MaxPoints.HasValue && currentPoints + trait.Cost > category.MaxPoints.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Applies a trait's effects to an entity.
    /// </summary>
    private void ApplyTrait(EntityUid player, TraitPrototype trait)
    {
        if (_whitelistSystem.IsWhitelistFail(trait.Whitelist, player) ||
            _whitelistSystem.IsBlacklistPass(trait.Blacklist, player))
            return;

        var transform = Transform(player);

        var effectCtx = new TraitEffectContext
        {
            Player = player,
            EntMan = EntityManager,
            Proto = _prototype,
            CompFactory = _factory,
            LogMan = _log,
            Transform = transform,
        };

        foreach (var effect in trait.Effects)
        {
            try
            {
                // Handle SpawnItemInHandEffect specially since it needs server-side systems
                if (effect is SpawnItemInHandEffect spawnEffect)
                    ApplySpawnItemEffect(player, spawnEffect, transform);
                else
                    effect.Apply(effectCtx);
            }
            catch (Exception e)
            {
                Log.Error($"Error applying effect {effect.GetType().Name} for trait {trait.ID}: {e}");
            }
        }

        // Legacy support: apply Components and TraitGear if Effects is empty
        if (trait.Effects.Count == 0)
        {
            if (trait.Components.Count > 0)
            {
                EntityManager.AddComponents(player, trait.Components, trait.RewriteComponents);
            }

            if (trait.TraitGear != null && TryComp(player, out HandsComponent? handsComponent))
            {
                var coords = transform.Coordinates;
                var item = Spawn(trait.TraitGear, coords);
                _hands.TryPickup(player, item, checkActionBlocker: false, handsComp: handsComponent);
            }
        }
    }

    /// <summary>
    /// Handles the SpawnItemInHandEffect since it requires server-side systems.
    /// </summary>
    private void ApplySpawnItemEffect(EntityUid player, SpawnItemInHandEffect effect, TransformComponent transform)
    {
        if (!TryComp<HandsComponent>(player, out var hands))
        {
            Log.Warning("Cannot spawn trait item: player has no hands component");
            return;
        }

<<<<<<< HEAD
        var coords = transform.Coordinates;
        var item = Spawn(effect.Item, coords);

        if (!_hands.TryPickup(player, item, checkActionBlocker: false, handsComp: hands))
            Log.Debug($"Could not pick up trait item {effect.Item}, leaving at feet");
=======
        foreach (var traitId in args.Profile.TraitPreferences)
        {
            if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                Log.Error($"No trait found with ID {traitId}!");
                return;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, args.Mob) ||
                _whitelistSystem.IsWhitelistPass(traitPrototype.Blacklist, args.Mob))
                continue;

            // Add all components required by the prototype
            if (traitPrototype.Components.Count > 0)
                EntityManager.AddComponents(args.Mob, traitPrototype.Components, false);

            // Add all JobSpecials required by the prototype
            foreach (var special in traitPrototype.Specials)
            {
                special.AfterEquip(args.Mob);
            }

            // Add item required by the trait
            if (traitPrototype.TraitGear == null)
                continue;

            if (!TryComp(args.Mob, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(args.Mob).Coordinates;
            var inhandEntity = Spawn(traitPrototype.TraitGear, coords);
            _sharedHandsSystem.TryPickup(args.Mob,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }
>>>>>>> upstreamwiz/master
    }
}
// Система полностью переписана под ADT, под новые трейты
