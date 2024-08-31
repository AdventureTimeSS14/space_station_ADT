using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.StatusIcon;
using Content.Shared.Antag;

namespace Content.Shared.ADT.Phantom.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class PhantomPuppetComponent : Component
{
    [DataField]
    public EntProtoId ClawsAction = "ActionPhantomPupClaws";

    [DataField, AutoNetworkedField]
    public EntityUid? ClawsActionEntity;

    [DataField]
    public EntProtoId HealAction = "ActionPhantomPupHeal";

    [DataField, AutoNetworkedField]
    public EntityUid? HealActionEntity;

    [DataField]
    public bool ClawsOn = false;

    [DataField]
    public EntityUid? Claws;

    [DataField]
    public float RegenerateBurnHealAmount = -40f;

    [DataField]
    public float RegenerateBruteHealAmount = -25f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "PhantomPuppetFaction";

    public override bool SessionSpecific => true;

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;
}
