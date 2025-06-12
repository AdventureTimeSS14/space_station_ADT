using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Supermatter.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterKudzuCollideComponent : Component
{
    [DataField]
    public EntProtoId CollisionResultPrototype = "Ash";

    [DataField]
    public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/ADT/Supermatter/supermatter.ogg");
}