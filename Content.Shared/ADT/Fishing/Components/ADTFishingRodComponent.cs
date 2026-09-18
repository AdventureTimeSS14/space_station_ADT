using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTFishingRodComponent : Component
{
    [DataField]
    public string BaitSlot = "bait";

    [DataField]
    public TimeSpan CastTime = TimeSpan.FromSeconds(10);

    [DataField]
    public float LoseChance = 0.2f;

    [DataField]
    public float FavoriteBaitChance = 0.7f;

    [DataField]
    public float HookSize = 0.22f;

    [DataField]
    public float Efficiency = 1f;

    [DataField]
    public EntProtoId Bobber = "ADTFishingBobber";

    [DataField]
    public SoundSpecifier ThrowSound = new SoundPathSpecifier("/Audio/ADT/Fishing/fishing_rod_throw.ogg");

    [DataField]
    public SoundSpecifier CatchSound = new SoundPathSpecifier("/Audio/ADT/Fishing/fishing_rod_catch.ogg");

    [ViewVariables]
    public EntityUid? ActiveBobber;
}
