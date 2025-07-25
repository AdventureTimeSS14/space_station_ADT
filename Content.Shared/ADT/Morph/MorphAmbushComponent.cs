using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Humanoid;

namespace Content.Shared.ADT.Morph;

[RegisterComponent]
public sealed partial class MorphAmbushComponent : Component
{
    /// <summary>
    /// время стана после касания, но не удара
    /// </summary>
    [DataField]
    public int StunTimeInteract = 6;
    /// <summary>
    /// урон при касании
    /// </summary>
    [DataField]
    public DamageSpecifier DamageOnTouch = new()
    {
        DamageDict = new()
        {
            { "Blunt", 20 },
            { "Slash", 20 },
        }
    };
}
