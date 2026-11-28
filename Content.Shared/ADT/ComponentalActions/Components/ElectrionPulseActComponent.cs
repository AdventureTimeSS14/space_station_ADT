using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ComponentalActions.Components;

[RegisterComponent]
public sealed partial class ElectrionPulseActComponent : Component, IComponentalActionComponent
{
    [DataField]
    public EntProtoId Action { get; set; } = "CompElectrionPulseAction";

    [DataField]
    public EntityUid? ActionEntity { get; set; }

    [DataField]
    public float Range = 7f;

    [DataField]
    public int ShockDamage = 10;

    [DataField]
    public TimeSpan ShockTime = TimeSpan.FromSeconds(15);

    [DataField]
    public float DespawnTime = 1.5f;

    [DataField]
    public LocId Emote = "componental-actions-vanish-emote";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");
}
