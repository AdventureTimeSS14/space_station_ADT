<<<<<<< ours
// ADT: Закомментировано из-за использования генокрада от Goob Station
// using Content.Shared.Cloning;
// using Robust.Shared.GameStates;
// using Robust.Shared.Prototypes;
||||||| base
using Content.Shared.Cloning;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
=======
using Content.Shared.Cloning;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
>>>>>>> theirs

// namespace Content.Shared.Changeling.Components;

<<<<<<< ours
// /// <summary>
// /// The storage component for Changelings, it handles the link between a changeling and its consumed identities
// /// that exist on a paused map.
// /// </summary>
// [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
// public sealed partial class ChangelingIdentityComponent : Component
// {
//     /// <summary>
//     /// The list of entities that exist on a paused map. They are paused clones of the victims that the ling has consumed, with all relevant components copied from the original.
//     /// The key is the EntityUid of the stored identity, the value is the original entity the identity came from.
//     /// The value will be set to null if that entity is deleted.
//     /// </summary>
//     // TODO: This should be handled via a relation system in the future.
//     [DataField, AutoNetworkedField]
//     public Dictionary<EntityUid, EntityUid?> ConsumedIdentities = new();
||||||| base
/// <summary>
/// The storage component for Changelings, it handles the link between a changeling and its consumed identities
/// that exist on a paused map.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ChangelingIdentityComponent : Component
{
    /// <summary>
    /// The list of entities that exist on a paused map. They are paused clones of the victims that the ling has consumed, with all relevant components copied from the original.
    /// The key is the EntityUid of the stored identity, the value is the original entity the identity came from.
    /// The value will be set to null if that entity is deleted.
    /// </summary>
    // TODO: This should be handled via a relation system in the future.
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, EntityUid?> ConsumedIdentities = new();
=======
/// <summary>
/// The storage component for Changelings, it handles the link between a changeling and its consumed identities
/// that exist on a paused map.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingIdentityComponent : Component
{
    /// <summary>
    /// List containing data regarding all devoured identities.
    /// The identities are paused clones of the victims that the ling has consumed, with all relevant components copied from the original.
    /// </summary>
    /// <remarks>
    /// Entries in this list do not get deleted for keeping track of total and unique identities.
    /// To check if an identity is valid compare <see cref="ChangelingIdentityData.Identity"/> to null.
    /// </remarks>
    [DataField]
    public List<ChangelingIdentityData> ConsumedIdentities = new();
>>>>>>> theirs

<<<<<<< ours
//     /// <summary>
//     /// The currently assumed identity.
//     /// </summary>
//     [DataField, AutoNetworkedField]
//     public EntityUid? CurrentIdentity;
||||||| base
    /// <summary>
    /// The currently assumed identity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentIdentity;
=======
    /// <summary>
    /// The currently assumed identity.
    /// </summary>
    [DataField]
    public EntityUid? CurrentIdentity;
>>>>>>> theirs

<<<<<<< ours
//     /// <summary>
//     /// The cloning settings passed to the CloningSystem, contains a list of all components to copy or have handled by their
//     /// respective systems.
//     /// </summary>
//     [DataField]
//     public ProtoId<CloningSettingsPrototype> IdentityCloningSettings = "ChangelingCloningSettings";
||||||| base
    /// <summary>
    /// The cloning settings passed to the CloningSystem, contains a list of all components to copy or have handled by their
    /// respective systems.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> IdentityCloningSettings = "ChangelingCloningSettings";
=======
    /// <summary>
    /// The cloning settings to use when cloning a devoured identity to the paused map.
    /// This contains a whitelist of all components that need to be backed up so that the changeling can transform into them later.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> IdentityCloningSettings = "ChangelingCloningSettings";
>>>>>>> theirs

<<<<<<< ours
//     public override bool SendOnlyToOwner => true;
// }
||||||| base
    public override bool SendOnlyToOwner => true;
}
=======
    /// <summary>
    /// Maximum number of stored disguises, including the changeling's starting identity.
    /// </summary>
    [DataField]
    public int MaxStoredDisguises = 5;

    public override bool SendOnlyToOwner => true;
}

[Serializable, NetSerializable]
public sealed class ChangelingIdentityComponentState : ComponentState
{
    public List<ChangelingNetworkedIdentityData> ConsumedIdentities;
    public NetEntity? CurrentIdentity;

    public ProtoId<CloningSettingsPrototype> IdentityCloningSettings;
    public int MaxStoredDisguises;

    public ChangelingIdentityComponentState(List<ChangelingNetworkedIdentityData> consumedIdentities,
        NetEntity? currentIdentity,
        ProtoId<CloningSettingsPrototype> identityCloningSettings,
        int maxStoredDisguises)
    {
        ConsumedIdentities = consumedIdentities;
        CurrentIdentity = currentIdentity;
        IdentityCloningSettings = identityCloningSettings;
        MaxStoredDisguises = maxStoredDisguises;
    }
}

/// <summary>
/// Stores data related to an identity a changeling has devoured.
/// </summary>
[DataDefinition]
public sealed partial class ChangelingIdentityData
{
    /// <summary>
    /// The stored identity used for cloning appearance and components.
    /// Set to null if the identity is ever deleted.
    /// </summary>
    [DataField]
    public EntityUid? Identity;

    /// <summary>
    /// The original entity that was devoured to obtain this identity.
    /// Set to null if the entity is ever deleted.
    /// </summary>
    [DataField]
    public EntityUid? Original;

    /// <summary>
    /// The mind of the original entity that was devoured to obtain this identity.
    /// Always null on Client.
    /// </summary>
    [DataField]
    public EntityUid? OriginalMind;

    /// <summary>
    /// Job prototype of the original entity at the time of devouring.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? OriginalJob;

    /// <summary>
    /// Name of the original entity at the time of being devoured.
    /// </summary>
    [DataField]
    public string OriginalName = "Unnamed";

    /// <summary>
    /// Whether this is the identity the entity started with.
    /// </summary>
    [DataField]
    public bool Starting = false;

    /// <summary>
    /// Whether this identity has granted DNA after devour.
    /// </summary>
    [DataField]
    public bool GrantedDna = false;

    /// <summary>
    /// Convert to a string representation. This if for logging & debugging. This is not localized and should not be
    /// shown to players.
    /// </summary>
    public override string ToString()
    {
        return $"{OriginalName} ({OriginalJob ?? "Unknown"}) - {Original}";
    }
}

/// <summary>
/// A net-serializable version of <see cref="ChangelingIdentityData"/> used for networking purposes.
/// It needs to be like this because EntityUid cannot be networked, so we convert it to NetEntity and send it over to the client using this class.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ChangelingNetworkedIdentityData
{
    [DataField]
    public NetEntity? Identity;

    [DataField]
    public NetEntity? Original;

    [DataField]
    public ProtoId<JobPrototype>? OriginalJob;

    [DataField]
    public string OriginalName = "";

    [DataField]
    public bool Starting;

    [DataField]
    public bool GrantedDna;
}

/// <summary>
/// Event raised on the changeling and broadcast when it gains a new identity, or an existing identity data is updated when gaining an identity.
/// Identities are never removed, so any future grants of an identity (after dropping it, for example) is an update.
/// </summary>
/// <param name="Changeling">The changeling that gained an identity.</param>
/// <param name="Identity">The identity data that was obtained.</param>
/// <param name="NewIdentity">Whether this is the first time an identity was gained.</param>
[ByRefEvent]
public record struct ChangelingGainedOrUpdatedIdentityEvent(EntityUid Changeling, ChangelingIdentityData Identity, bool NewIdentity);
>>>>>>> theirs
