using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Components.Equipment;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeScannerComponent : Component
{
    [DataField]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");
}