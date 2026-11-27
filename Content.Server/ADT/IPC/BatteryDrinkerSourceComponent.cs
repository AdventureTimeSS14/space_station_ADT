using Robust.Shared.Audio;

namespace Content.Server.ADT.IPC;

[RegisterComponent]
public sealed partial class BatteryDrinkerSourceComponent : Component
{
    [DataField]
    public int? MaxAmount;

    [DataField]
    public float DrinkSpeedMulti = 1f;

    [DataField]
    public SoundSpecifier? DrinkSound;
}
