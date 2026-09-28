// ADT: Закомментировано из-за использования генокрада от Goob Station
// using Robust.Shared.GameStates;

// namespace Content.Shared.Changeling.Components;

<<<<<<< ours
// /// <summary>
// /// Component used for marking entities devoured by a changeling.
// /// Used to prevent granting the identity several times.
// /// </summary>
// [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
// public sealed partial class ChangelingDevouredComponent : Component
// {
//     /// <summary>
//     /// HashSet of all changelings that have devoured this entity.
//     /// </summary>
//     // TODO: This should be using some sort of relation system in the future.
//     [DataField, AutoNetworkedField]
//     public HashSet<EntityUid> DevouredBy = new();
// }
||||||| base
/// <summary>
/// Component used for marking entities devoured by a changeling.
/// Used to prevent granting the identity several times.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingDevouredComponent : Component
{
    /// <summary>
    /// HashSet of all changelings that have devoured this entity.
    /// </summary>
    // TODO: This should be using some sort of relation system in the future.
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> DevouredBy = new();
}
=======
/// <summary>
/// Component used for marking entities devoured by a changeling.
/// Used to track which changelings have an identity of this entity.
/// Used for cleanup purposes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingDevouredComponent : Component
{
    /// <summary>
    /// HashSet of all changelings that have devoured this entity.
    /// </summary>
    // TODO: This should be using some sort of relation system in the future.
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> DevouredBy = new();
}
>>>>>>> theirs
