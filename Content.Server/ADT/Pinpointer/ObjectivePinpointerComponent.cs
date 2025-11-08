using Content.Server.ADT.Pinpointer.Systems;

namespace Content.Server.ADT.Pinpointer;

/// <summary>
/// Pinpointer that can track targets from antagonist objectives
/// </summary>
[RegisterComponent, Access(typeof(ObjectivePinpointerSystem))]
public sealed partial class ObjectivePinpointerComponent : Component
{
    /// <summary>
    /// Mind entity that owns this pinpointer, used to access objectives
    /// </summary>S
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? OwnerMind;
}