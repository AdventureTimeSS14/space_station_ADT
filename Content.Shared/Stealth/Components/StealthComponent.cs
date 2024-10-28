using Content.Shared.ADT.Stealth.Components;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Stealth.Components;
/// <summary>
/// Add this component to an entity that you want to be cloaked.
/// It overlays a shader on the entity to give them an invisibility cloaked effect.
/// It also turns the entity invisible.
/// Use other components (like StealthOnMove) to modify this component's visibility based on certain conditions.
/// </summary>
[RegisterComponent, NetworkedComponent]
//[Access(typeof(SharedStealthSystem))] // ADT commented
public sealed partial class StealthComponent : BaseStealthComponent // ADT: Сделал базовый компонент стелса для корректной работы стелса генокрада. WhitelistSystem не помог сделать компонент универсальным.
{
}

[Serializable, NetSerializable]
public sealed class StealthComponentState : ComponentState
{
    public readonly float Visibility;
    public readonly TimeSpan? LastUpdated;
    public readonly bool Enabled;
    public readonly string Desc;    // ADT

    public StealthComponentState(float stealthLevel, TimeSpan? lastUpdated, bool enabled, string desc) // ADT
    {
        Visibility = stealthLevel;
        LastUpdated = lastUpdated;
        Enabled = enabled;
        Desc = desc;    // ADT
    }
}
