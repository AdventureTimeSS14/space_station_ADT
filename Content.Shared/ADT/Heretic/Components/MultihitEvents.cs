using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Heretic.Components;

/// <summary>
///     ADT: from Goob Multihit, base condition on the wielder.
/// </summary>
[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class BaseMultihitUserConditionEvent : HandledEntityEventArgs
{
    public EntityUid User = EntityUid.Invalid;
}

/// <summary>
///     Wielder must pass a whitelist.
/// </summary>
public sealed partial class MultihitUserWhitelistEvent : BaseMultihitUserConditionEvent
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    [DataField]
    public bool Blacklist;
}

/// <summary>
///     Wielder must be a heretic on a given path/stage.
/// </summary>
public sealed partial class MultihitUserHereticEvent : BaseMultihitUserConditionEvent
{
    [DataField]
    public int MinPathStage;

    [DataField]
    public string? RequiredPath;
}
