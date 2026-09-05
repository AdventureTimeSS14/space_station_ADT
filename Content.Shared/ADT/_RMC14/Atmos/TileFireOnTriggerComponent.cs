// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TileFireOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField, AutoNetworkedField]
    public int Range = 2;

    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCTileFire";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("GlassBreak");

    [DataField, AutoNetworkedField]
    public int? Intensity;

    [DataField, AutoNetworkedField]
    public int? Duration;
}
