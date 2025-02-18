using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.CantShoot;

[RegisterComponent]
public sealed partial class CantShootComponent : Component
{
    [DataField]
    public string? Popup;

    [DataField]
    public EntityWhitelist? Whitelist;
}
