using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Eui;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.Phantom.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PhantomPortalComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? LinkedPortal;

    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxTeleportRadius = 15f;
}
