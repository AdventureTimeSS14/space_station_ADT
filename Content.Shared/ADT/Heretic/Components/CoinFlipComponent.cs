//

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CoinFlipComponent : Component
{
    [DataField(required: true)]
    public string FlippingSpriteState = "flip";

    [DataField(required: true)]
    public List<CoinSide> Sides = new();

    [DataField, AutoNetworkedField]
    public CoinSide? CurrentSide;

    [DataField(required: true)]
    public TimeSpan FlipTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan FlipDelay = TimeSpan.FromMilliseconds(250);

    [DataField, AutoNetworkedField]
    public bool IsFlipping;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan FlipEndTime;

    [DataField]
    public SoundSpecifier FlipSound = new SoundPathSpecifier("/Audio/ADT/Heretic/coinflip.ogg");

    [DataField]
    public EntityUid? User;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CoinSide
{
    [DataField]
    public LocId Name = string.Empty;

    [DataField]
    public string SpriteState = string.Empty;

    [DataField]
    public string Effect = string.Empty;
}

[Serializable, NetSerializable]
public enum CoinFlipVisuals : byte
{
    SpriteState,
}

[Serializable, NetSerializable]
public enum CoinFlipKey : byte
{
    Key,
}
