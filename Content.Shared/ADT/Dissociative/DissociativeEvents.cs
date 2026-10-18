using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Dissociative;

public sealed partial class DissociativeManifestActionEvent : InstantActionEvent;

public sealed partial class DissociativeFrontActionEvent : InstantActionEvent;

public sealed partial class DissociativeReleaseActionEvent : InstantActionEvent;

public sealed partial class DissociativeSetupActionEvent : InstantActionEvent;

public sealed partial class DissociativeSurrenderActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum DissociativeAlterSetupUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DissociativeAlterSetupUiState(string name, Sex sex) : BoundUserInterfaceState
{
    public readonly string Name = name;
    public readonly Sex Sex = sex;
}

[Serializable, NetSerializable]
public sealed class DissociativeAlterSetSexMessage(Sex sex) : BoundUserInterfaceMessage
{
    public readonly Sex Sex = sex;
}

[Serializable, NetSerializable]
public sealed class DissociativeAlterOpenCustomizationMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DissociativeAlterConfirmMessage(string name) : BoundUserInterfaceMessage
{
    public readonly string Name = name;
}
