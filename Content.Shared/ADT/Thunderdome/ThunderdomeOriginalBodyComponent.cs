using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.ADT.Thunderdome;

[RegisterComponent]
public sealed partial class ThunderdomeOriginalBodyComponent : Component
{
    /// <summary>
    /// The user whose original body this is (their mind is temporarily on the arena).
    /// </summary>
    [DataField]
    public NetUserId OriginalOwner;
}
