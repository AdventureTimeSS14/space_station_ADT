using Content.Shared.Stealth;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Stealth.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedStealthSystem))]
public sealed partial class DigitalCamouflageComponent : Component
{
}
