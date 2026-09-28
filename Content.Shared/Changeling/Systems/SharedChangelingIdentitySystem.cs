<<<<<<< HEAD
// ADT: Закомментировано из-за использования генокрада от Goob Station
// using System.Linq;
// using System.Numerics;
// using Content.Shared.Body;
// using Content.Shared.Changeling.Components;
// using Content.Shared.Cloning;
// using Content.Shared.Humanoid;
// using Content.Shared.NameModifier.EntitySystems;
// using Robust.Shared.GameStates;
// using Robust.Shared.Map;
// using Robust.Shared.Network;
// using Robust.Shared.Player;
// using Robust.Shared.Prototypes;
=======
﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Changeling.Components;
using Content.Shared.Cloning;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Roles.Jobs;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
>>>>>>> wizards-filtered

// namespace Content.Shared.Changeling.Systems;

<<<<<<< HEAD
// public abstract class SharedChangelingIdentitySystem : EntitySystem
// {
//     [Dependency] private readonly INetManager _net = default!;
//     [Dependency] private readonly IPrototypeManager _prototype = default!;
//     [Dependency] private readonly MetaDataSystem _metaSystem = default!;
//     [Dependency] private readonly NameModifierSystem _nameMod = default!;
//     [Dependency] private readonly SharedCloningSystem _cloningSystem = default!;
//     [Dependency] private readonly SharedMapSystem _map = default!;
//     [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
//     [Dependency] private readonly SharedPvsOverrideSystem _pvsOverrideSystem = default!;
=======
public abstract partial class SharedChangelingIdentitySystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private MetaDataSystem _metaSystem = default!;
    [Dependency] private SharedCloningSystem _cloningSystem = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPvsOverrideSystem _pvsOverrideSystem = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedJobSystem _job = default!;
>>>>>>> wizards-filtered

//     public MapId? PausedMapId;

//     public override void Initialize()
//     {
//         base.Initialize();

<<<<<<< HEAD
//         SubscribeLocalEvent<ChangelingIdentityComponent, MapInitEvent>(OnMapInit);
//         SubscribeLocalEvent<ChangelingIdentityComponent, ComponentShutdown>(OnShutdown);
//         SubscribeLocalEvent<ChangelingIdentityComponent, PlayerAttachedEvent>(OnPlayerAttached);
//         SubscribeLocalEvent<ChangelingIdentityComponent, PlayerDetachedEvent>(OnPlayerDetached);
//         SubscribeLocalEvent<ChangelingStoredIdentityComponent, ComponentRemove>(OnStoredRemove);

//         SubscribeLocalEvent<ChangelingDevouredComponent, ComponentShutdown>(OnDevouredShutdown);
//     }
=======
        SubscribeLocalEvent<ChangelingIdentityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChangelingIdentityComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ChangelingIdentityComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<ChangelingIdentityComponent, ChangelingDevouredEvent>(OnDevouredEntity);
        SubscribeLocalEvent<ChangelingStoredIdentityComponent, ComponentRemove>(OnStoredRemove);

        SubscribeLocalEvent<ChangelingDevouredComponent, ComponentShutdown>(OnDevouredShutdown);
        SubscribeLocalEvent<RecentlyDevouredComponent, MobStateChangedEvent>(OnRecentlyDevouredMobState);
    }

    private void OnDevouredEntity(Entity<ChangelingIdentityComponent> ent, ref ChangelingDevouredEvent args)
    {
        if (args.ObtainedIdentity)
        {
            GrantIdentity(ent.Owner, args.Devoured, true);
        }

        if (args.GrantedDna && TryGetDataFromOriginal(ent.AsNullable(), args.Devoured, out var data))
        {
            data.GrantedDna = true;
        }
    }
>>>>>>> wizards-filtered

//     private void OnPlayerAttached(Entity<ChangelingIdentityComponent> ent, ref PlayerAttachedEvent args)
//     {
//         HandOverPvsOverride(ent, args.Player);
//     }

//     private void OnPlayerDetached(Entity<ChangelingIdentityComponent> ent, ref PlayerDetachedEvent args)
//     {
//         CleanupPvsOverride(ent, args.Player);
//     }

<<<<<<< HEAD
//     private void OnMapInit(Entity<ChangelingIdentityComponent> ent, ref MapInitEvent args)
//     {
//         // Make a backup of our current identity so we can transform back.
//         var clone = CloneToPausedMap(ent, ent.Owner);
//         ent.Comp.CurrentIdentity = clone;
//     }
=======
    private void OnMapInit(Entity<ChangelingIdentityComponent> ent, ref MapInitEvent args)
    {
        // Make a backup of our current identity so we can transform back.
        GrantIdentity(ent.Owner, ent.Owner);

        if (!TryGetDataFromOriginal(ent.AsNullable(), ent, out var data))
            return;

        ent.Comp.CurrentIdentity = data.Identity;
    }
>>>>>>> wizards-filtered

//     private void OnShutdown(Entity<ChangelingIdentityComponent> ent, ref ComponentShutdown args)
//     {
//         if (TryComp<ActorComponent>(ent, out var actor))
//             CleanupPvsOverride(ent, actor.PlayerSession);

//         CleanupChangelingNullspaceIdentities(ent);
//         CleanupDevouredReferences(ent);
//     }

//     // Set all references to this entity to null to prevent PVS errors when networking.
//     private void OnDevouredShutdown(Entity<ChangelingDevouredComponent> ent, ref ComponentShutdown args)
//     {
//         foreach (var ling in ent.Comp.DevouredBy)
//         {
//             if (!TryComp<ChangelingIdentityComponent>(ling, out var identityComp))
//                 continue;

<<<<<<< HEAD
//             var keysToUpdate = identityComp.ConsumedIdentities
//                 .Where(kvp => kvp.Value == ent.Owner)
//                 .Select(kvp => kvp.Key)
//                 .ToList();

//             if (keysToUpdate.Count == 0)
//                 continue; // No need to dirty.

//             foreach (var key in keysToUpdate)
//                 identityComp.ConsumedIdentities[key] = null;

//             Dirty(ling, identityComp);
//         }
//     }

//     private void OnStoredRemove(Entity<ChangelingStoredIdentityComponent> ent, ref ComponentRemove args)
//     {
//         // The last stored identity is being deleted, we can clean up the map.
//         if (_net.IsServer && PausedMapId != null && Count<ChangelingStoredIdentityComponent>() <= 1)
//             _map.QueueDeleteMap(PausedMapId.Value);
//     }
=======
            RemoveOriginalReference((ling, identityComp), ent);
        }
    }

    private void OnRecentlyDevouredMobState(Entity<RecentlyDevouredComponent> ent, ref MobStateChangedEvent args)
    {
        // Once we are revived the body is no longer recently devoured.
        if (args.NewMobState != MobState.Alive)
            return;

        RemCompDeferred<RecentlyDevouredComponent>(ent);
    }

    private void OnStoredRemove(Entity<ChangelingStoredIdentityComponent> ent, ref ComponentRemove args)
    {
        // The last stored identity is being deleted, we can clean up the map.
        if (_net.IsServer && PausedMapId != null && Count<ChangelingStoredIdentityComponent>() <= 1)
            _map.QueueDeleteMap(PausedMapId.Value);
    }
>>>>>>> wizards-filtered

//     /// <summary>
//     /// Cleanup all nullspaced Identities when the changeling no longer exists
//     /// </summary>
//     /// <param name="ent">the changeling</param>
//     public void CleanupChangelingNullspaceIdentities(Entity<ChangelingIdentityComponent> ent)
//     {
//         if (_net.IsClient)
//             return;

<<<<<<< HEAD
//         foreach (var consumedIdentity in ent.Comp.ConsumedIdentities)
//         {
//             QueueDel(consumedIdentity.Key);
//         }
//     }

//     /// <summary>
//     /// Removes all references to the owning changeling from ChangelingDevouredComponents.
//     /// </summary>
//     /// <param name="ent">The changeling entity</param>
//     private void CleanupDevouredReferences(Entity<ChangelingIdentityComponent> ent)
//     {
//         foreach (var devouredUid in ent.Comp.ConsumedIdentities.Values)
//         {
//             if (!TryComp<ChangelingDevouredComponent>(devouredUid, out var devouredComp))
//                 continue;

//             if (devouredComp.DevouredBy.Remove(ent.Owner))
//                 Dirty(devouredUid.Value, devouredComp);
//         }
//     }

//     /// <summary>
//     /// Clone a target humanoid to a paused map.
//     /// It creates a perfect copy of the target and can be used to pull components down for future use.
//     /// </summary>
//     /// <param name="settings">The settings to use for cloning.</param>
//     /// <param name="target">The target to clone.</param>
//     public EntityUid? CloneToPausedMap(CloningSettingsPrototype settings, EntityUid target)
//     {
//         // Don't create client side duplicate clones or a clientside map.
//         if (_net.IsClient)
//             return null;

//         if (!TryComp<HumanoidProfileComponent>(target, out var humanoid)
//             || !_prototype.Resolve(humanoid.Species, out var speciesPrototype))
//             return null;

//         EnsurePausedMap();
//         var clone = Spawn(speciesPrototype.Prototype, new MapCoordinates(Vector2.Zero, PausedMapId!.Value));

//         var storedIdentity = EnsureComp<ChangelingStoredIdentityComponent>(clone);
//         storedIdentity.OriginalEntity = target; // TODO: network this once we have WeakEntityReference or the autonetworking source gen is fixed
=======
        foreach (var consumedIdentity in ent.Comp.ConsumedIdentities)
        {
            QueueDel(consumedIdentity.Identity);
        }
    }

    /// <summary>
    /// Removes all references to the owning changeling from ChangelingDevouredComponents.
    /// </summary>
    /// <param name="ent">The changeling entity</param>
    private void CleanupDevouredReferences(Entity<ChangelingIdentityComponent> ent)
    {
        foreach (var devouredUid in ent.Comp.ConsumedIdentities)
        {
            if (!TryComp<ChangelingDevouredComponent>(devouredUid.Original, out var devouredComp))
                continue;

            if (devouredComp.DevouredBy.Remove(ent.Owner))
                Dirty(devouredUid.Original.Value, devouredComp);
        }
    }

    /// <summary>
    /// Removes reference to an original entity from <see cref="ChangelingIdentityComponent"/>.
    /// </summary>
    /// <param name="ent">The changeling.</param>
    /// <param name="original">The entity to remove from identity originals.</param>
    private void RemoveOriginalReference(Entity<ChangelingIdentityComponent> ent, EntityUid original)
    {
        foreach (var identity in ent.Comp.ConsumedIdentities)
        {
            if (identity.Original == original)
                identity.Original = null;
        }

        Dirty(ent);
    }

    /// <summary>
    /// Clone a target humanoid to a paused map.
    /// It creates a perfect copy of the target and can be used to pull components down for future use.
    /// </summary>
    /// <param name="settings">The settings to use for cloning.</param>
    /// <param name="target">The target to clone.</param>
    public EntityUid? CloneToPausedMap(ProtoId<CloningSettingsPrototype> settings, EntityUid target)
    {
        // Don't create client side duplicate clones or a clientside map.
        if (_net.IsClient)
            return null;

        EnsurePausedMap();
        if (PausedMapId == null)
            return null;

        var mapCoords = new MapCoordinates(0, 0, PausedMapId.Value);
        if (!_cloningSystem.TryCloning(target, mapCoords, settings, out var clone))
            return null;

        var storedIdentity = EnsureComp<ChangelingStoredIdentityComponent>(clone.Value);
        storedIdentity.OriginalEntity = target; // TODO: network this once we have a relations system so that this does not cause PVS errors.
>>>>>>> wizards-filtered

//         if (TryComp<ActorComponent>(target, out var actor))
//             storedIdentity.OriginalSession = actor.PlayerSession;

<<<<<<< HEAD
//         _visualBody.CopyAppearanceFrom(target, clone);
//         _cloningSystem.CloneComponents(target, clone, settings);

//         var targetName = _nameMod.GetBaseName(target);
//         _metaSystem.SetEntityName(clone, targetName);

//         return clone;
//     }

//     /// <summary>
//     /// Clone a target humanoid to a paused map and add it to the Changelings list of identities.
//     /// It creates a perfect copy of the target and can be used to pull components down for future use.
//     /// </summary>
//     /// <param name="ent">The Changeling.</param>
//     /// <param name="target">The target to clone.</param>
//     public EntityUid? CloneToPausedMap(Entity<ChangelingIdentityComponent> ent, EntityUid target)
//     {
//         if (!_prototype.Resolve(ent.Comp.IdentityCloningSettings, out var settings))
//             return null;

//         var clone = CloneToPausedMap(settings, target);
=======
        return clone;
    }

    /// <summary>
    /// Clone a target humanoid to a paused map and add it to the Changelings list of identities.
    /// It creates a perfect copy of the target and can be used to pull components down for future use.
    /// </summary>
    /// <param name="ent">The Changeling.</param>
    /// <param name="target">The target to clone.</param>
    /// <param name="devoured">Whether this identity was granted via devouring, aka granted DNA to the changeling.</param>
    public EntityUid? GrantIdentity(Entity<ChangelingIdentityComponent?> ent, EntityUid target, bool devoured = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return null;

        var clone = CloneToPausedMap(ent.Comp.IdentityCloningSettings, target);
>>>>>>> wizards-filtered

//         if (clone == null)
//             return null;

<<<<<<< HEAD
//         ent.Comp.ConsumedIdentities.Add(clone.Value, target);

//         Dirty(ent);
//         HandlePvsOverride(ent, clone.Value);
=======
        var isNew = false;

        // We see if we already have a identity slot for this entity.
        // This can happen if we devoured them before, but then dropped their stored identity.
        if (!TryGetDataFromOriginal(ent.AsNullable(), target, out var newIdentity))
        {
            newIdentity = new ChangelingIdentityData();
            ent.Comp.ConsumedIdentities.Add(newIdentity);
            isNew = true; // Data didn't exist before so its not an update.
        }

        UpdateIdentityData(newIdentity, clone.Value, target);
        AddDevouredReference(ent!, target);

        newIdentity.GrantedDna = devoured;

        if (newIdentity.Original == ent)
        {
            // This is an identity of us, we don't wanna count it for objectives n stuff.
            newIdentity.Starting = true;
            newIdentity.GrantedDna = true;
        }

        var ev = new ChangelingGainedOrUpdatedIdentityEvent(ent, newIdentity, isNew);
        RaiseLocalEvent(ent, ref ev, true); // Broadcast it to allow the mind tracker to update.

        HandlePvsOverride(ent, clone.Value);
        Dirty(ent);
>>>>>>> wizards-filtered

//         return clone;
//     }

<<<<<<< HEAD
//     /// <summary>
//     /// Simple helper to add a PVS override to a nullspace identity.
//     /// </summary>
//     /// <param name="uid">The actor that should get the override.</param>
//     /// <param name="identity">The identity stored in nullspace.</param>
//     private void HandlePvsOverride(EntityUid uid, EntityUid identity)
//     {
//         if (!TryComp<ActorComponent>(uid, out var actor))
//             return;
=======
    /// <summary>
    /// Marks that the changeling has successfully devoured the target.
    /// </summary>
    public void AddDevouredReference(Entity<ChangelingIdentityComponent> ent, EntityUid target)
    {
        var targetDevoured = EnsureComp<ChangelingDevouredComponent>(target);
        targetDevoured.DevouredBy.Add(ent.Owner);

        Dirty(target, targetDevoured);
    }

    /// <summary>
    /// Drop a stored identity from the changeling's storage.
    /// </summary>
    public void DropStoredIdentity(Entity<ChangelingIdentityComponent?> ent, EntityUid identity)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!HasComp<ChangelingStoredIdentityComponent>(identity))
            return; // Not a stored identity.

        var toDrop = ent.Comp.ConsumedIdentities.Where(data => data.Identity == identity).ToList();

        foreach (var dropped in toDrop)
        {
            if (TryComp<ChangelingDevouredComponent>(dropped.Original, out var devoured))
            {
                if (devoured.DevouredBy.Remove(ent))
                    Dirty(dropped.Original.Value, devoured);
            }

            dropped.Identity = null;
        }

        PredictedQueueDel(identity);

        if (toDrop.Count > 0)
        {
            Dirty(ent);
        }
    }

    /// <summary>
    /// Simple helper to add a PVS override to a nullspace identity.
    /// </summary>
    /// <param name="uid">The actor that should get the override.</param>
    /// <param name="identity">The identity stored in nullspace.</param>
    private void HandlePvsOverride(EntityUid uid, EntityUid identity)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;
>>>>>>> wizards-filtered

//         _pvsOverrideSystem.AddSessionOverride(identity, actor.PlayerSession);
//     }

<<<<<<< HEAD
//     /// <summary>
//     /// Cleanup all PVS overrides for the owner of the ChangelingIdentity
//     /// </summary>
//     /// <param name="ent">The changeling storing the identities.</param>
//     /// <param name="session">The session you wish to remove the overrides from.</param>
//     private void CleanupPvsOverride(Entity<ChangelingIdentityComponent> ent, ICommonSession session)
//     {
//         foreach (var identity in ent.Comp.ConsumedIdentities)
//         {
//             _pvsOverrideSystem.RemoveSessionOverride(identity.Key, session);
//         }
//     }

//     /// <summary>
//     /// Inform another session of the entities stored for transformation.
//     /// </summary>
//     /// <param name="ent">The changeling storing the identities.</param>
//     /// <param name="session">The session you wish to inform.</param>
//     public void HandOverPvsOverride(Entity<ChangelingIdentityComponent> ent, ICommonSession session)
//     {
//         foreach (var identity in ent.Comp.ConsumedIdentities)
//         {
//             _pvsOverrideSystem.AddSessionOverride(identity.Key, session);
//         }
//     }

//     /// <summary>
//     /// Create a paused map for storing devoured identities as a clone of the player.
//     /// </summary>
//     private void EnsurePausedMap()
//     {
//         if (_map.MapExists(PausedMapId))
//             return;

//         var mapUid = _map.CreateMap(out var newMapId);
//         _metaSystem.SetEntityName(mapUid, Loc.GetString("changeling-paused-map-name"));
//         PausedMapId = newMapId;
//         _map.SetPaused(mapUid, true);
//     }
// }
=======
    /// <summary>
    /// Cleanup all PVS overrides for the owner of the ChangelingIdentity
    /// </summary>
    /// <param name="ent">The changeling storing the identities.</param>
    /// <param name="session">The session you wish to remove the overrides from.</param>
    private void CleanupPvsOverride(Entity<ChangelingIdentityComponent> ent, ICommonSession session)
    {
        foreach (var identity in ent.Comp.ConsumedIdentities)
        {
            if (identity.Identity == null)
                continue;

            _pvsOverrideSystem.RemoveSessionOverride(identity.Identity.Value, session);
        }
    }

    /// <summary>
    /// Inform another session of the entities stored for transformation.
    /// </summary>
    /// <param name="ent">The changeling storing the identities.</param>
    /// <param name="session">The session you wish to inform.</param>
    public void HandOverPvsOverride(Entity<ChangelingIdentityComponent> ent, ICommonSession session)
    {
        foreach (var identity in ent.Comp.ConsumedIdentities)
        {
            if (identity.Identity == null)
                continue;

            _pvsOverrideSystem.AddSessionOverride(identity.Identity.Value, session);
        }
    }

    /// <summary>
    /// Returns whether the changeling has space to store another disguise.
    /// </summary>
    public bool HasFreeDisguiseSlot(Entity<ChangelingIdentityComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.ConsumedIdentities.Count(data => data.Identity != null) < ent.Comp.MaxStoredDisguises;
    }

    /// <summary>
    /// Whether the given changeling has a valid identity of the given entity.
    /// </summary>
    public bool HasIdentity(Entity<ChangelingIdentityComponent?> changeling, EntityUid devoured)
    {
        if (!Resolve(changeling, ref changeling.Comp, false))
            return false;

        return changeling.Comp.ConsumedIdentities.FirstOrDefault(data => data.Original == devoured && data.Identity != null) != null;
    }

    /// <summary>
    /// Create a paused map for storing devoured identities as a clone of the player.
    /// </summary>
    private void EnsurePausedMap()
    {
        if (_map.MapExists(PausedMapId))
            return;

        var mapUid = _map.CreateMap(out var newMapId);
        _metaSystem.SetEntityName(mapUid, Loc.GetString("changeling-paused-map-name"));
        PausedMapId = newMapId;
        _map.SetPaused(mapUid, true);
    }

    /// <summary>
    /// Creates a ChangelingIdentityData for given entities.
    /// </summary>
    /// <param name="identity">The created identity this is supposed to refer to.</param>
    /// <param name="original">The original entity this is supposed to refer to.</param>
    /// <returns>Identity data based on the given parameters.</returns>
    public ChangelingIdentityData CreateIdentityData(EntityUid identity, EntityUid original)
    {
        ChangelingIdentityData identityData = new ChangelingIdentityData();
        UpdateIdentityData(identityData, identity, original);

        return identityData;
    }

    /// <summary>
    /// Updates an existing identity data with information from a new identity and original entity.
    /// </summary>
    /// <param name="data">The existing data.</param>
    /// <param name="identity">The changeling identity to use.</param>
    /// <param name="original">The original entity of the identity.</param>
    public void UpdateIdentityData(ChangelingIdentityData data, EntityUid identity, EntityUid original)
    {
        data.Identity = identity;
        data.Original = original;
        data.OriginalName = Name(original);

        var foundMind = _mind.TryGetMind(original, out var mindId, out _);
        data.OriginalMind = foundMind ? mindId : null;

        if (foundMind)
        {
            _job.MindTryGetJobId(mindId, out var jobId);
            data.OriginalJob = jobId;
        }
    }

    /// <summary>
    /// Fetches the relevant <see cref="ChangelingIdentityData"/> from an entity's <see cref="ChangelingIdentityComponent"/> based on the identity's EntityUid.
    /// </summary>
    /// <param name="ent">The changeling entity.</param>
    /// <param name="identity">The identity's EntityUid.</param>
    /// <param name="identityData">The returned <see cref="ChangelingIdentityData"/> if one is found.</param>
    /// <returns>True if identity data is found, otherwise False.</returns>
    public bool TryGetDataFromIdentity(Entity<ChangelingIdentityComponent?> ent, EntityUid identity, [NotNullWhen(true)] out ChangelingIdentityData? identityData)
    {
        identityData = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        identityData = ent.Comp.ConsumedIdentities.FirstOrDefault(data => data.Identity == identity);

        return identityData != null;
    }

    /// <summary>
    /// Fetches the relevant <see cref="ChangelingIdentityData"/> from an entity's <see cref="ChangelingIdentityComponent"/> based on the original entity's EntityUid.
    /// </summary>
    /// <param name="ent">The changeling entity.</param>
    /// <param name="original">The original entity's EntityUid.</param>
    /// <param name="identityData">The returned <see cref="ChangelingIdentityData"/> if one is found.</param>
    /// <returns>True if identity data is found, otherwise False.</returns>
    public bool TryGetDataFromOriginal(Entity<ChangelingIdentityComponent?> ent, EntityUid original, [NotNullWhen(true)] out ChangelingIdentityData? identityData)
    {
        identityData = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        identityData = ent.Comp.ConsumedIdentities.FirstOrDefault(data => data.Original == original);

        return identityData != null;
    }

    /// <summary>
    /// Fetches the <see cref="ChangelingIdentityData"/> from an entity's <see cref="ChangelingIdentityComponent"/> based on the identity they are currently using.
    /// </summary>
    /// <param name="ent">The changeling entity.</param>
    /// <param name="identityData">The returned <see cref="ChangelingIdentityData"/> for the current identity if one is found.</param>
    /// <returns>True if identity data is found, otherwise False.</returns>
    public bool TryGetCurrentIdentityData(Entity<ChangelingIdentityComponent?> ent, [NotNullWhen(true)] out ChangelingIdentityData? identityData)
    {
        identityData = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.CurrentIdentity == null)
            return false;

        identityData = ent.Comp.ConsumedIdentities.FirstOrDefault(data => data.Identity == ent.Comp.CurrentIdentity);

        return identityData != null;
    }
}
>>>>>>> wizards-filtered
