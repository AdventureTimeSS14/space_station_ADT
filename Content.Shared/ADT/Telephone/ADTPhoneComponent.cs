using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Telephone;

/// <summary>
/// Handheld telephone for the quartermaster and salvage specialists.
/// Works on top of the vanilla telephone system.
/// </summary>
[RegisterComponent]
public sealed partial class ADTPhoneComponent : Component
{
    [DataField]
    public bool Dnd;

    [DataField]
    public TimeSpan CallCooldown = TimeSpan.FromSeconds(1.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCall;
}
