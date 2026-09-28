// ADT: Закомментировано из-за использования генокрада от Goob Station
// using Robust.Shared.Serialization;

// namespace Content.Shared.Changeling.Systems;

<<<<<<< ours
// /// <summary>
// /// Send when a player selects an intentity to transform into in the radial menu.
// /// </summary>
// [Serializable, NetSerializable]
// public sealed class ChangelingTransformIdentitySelectMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
// {
//     /// <summary>
//     /// The uid of the cloned identity.
//     /// </summary>
//     public readonly NetEntity TargetIdentity = targetIdentity;
// }
||||||| base
/// <summary>
/// Send when a player selects an intentity to transform into in the radial menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentitySelectMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The uid of the cloned identity.
    /// </summary>
    public readonly NetEntity TargetIdentity = targetIdentity;
}
=======
/// <summary>
/// Send when a player selects an identity to transform into in the radial menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentitySelectMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The uid of the stored identity.
    /// </summary>
    public readonly NetEntity TargetIdentity = targetIdentity;
}

/// <summary>
/// Send when a player selects an identity to drop from their storage.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentityDropMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The uid of the stored identity.
    /// </summary>
    public readonly NetEntity TargetIdentity = targetIdentity;
}
>>>>>>> theirs

// [Serializable, NetSerializable]
// public enum ChangelingTransformUiKey : byte
// {
//     Key,
// }
