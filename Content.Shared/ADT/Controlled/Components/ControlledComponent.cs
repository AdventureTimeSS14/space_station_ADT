using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.Controlled;


[RegisterComponent, NetworkedComponent]
public sealed partial class ControlledComponent : Component
{
    /// <summary>
    /// Entity that stole control. 
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Controller = new();

    /// <summary>
    /// PseudoEntity for player that owns controlled entity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Observer = new();

    /// <summary>
    /// Accumulator... For Update()
    /// </summary>
    [ViewVariables]
    public float Accumulator = 0;

    /// <summary>
    /// Duration of control effect (in seconds)
    /// </summary>
    [ViewVariables]
    public float Duration = 20f;

    /// <summary>
    /// Control priority. If higher than in exsisting ControlledComponent, will steal stolen control. ((bruh))
    /// </summary>
    [ViewVariables]
    public int Priority = 1;

    /// <summary>
    /// Keyword for specific controlling entities like phantom
    /// </summary>
    [ViewVariables]
    public string Key = "Default";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ControlledObserverComponent : Component
{
    /// <summary>
    /// Parent entity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Source = new();
}
