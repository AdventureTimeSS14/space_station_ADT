using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.AutoSurgeon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoSurgeonComponent : Component
{
    public const string SlotId = "autosurgeon_slot";

    [DataField]
    public List<EntProtoId> StartingItems = new();

    [DataField(required: true)]
    public List<ItemSlot> ImplantSlots = new();

    [DataField, AutoNetworkedField]
    public int Uses = -1;

    [DataField]
    public List<AutoSurgeonStep> Steps = new();

    [DataField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/ADT/Machines/lathe_print.ogg");

    [ViewVariables]
    public int CurrentStep;

    [ViewVariables]
    public EntityUid? CurrentPatient;

    [ViewVariables]
    public DoAfterId? ActiveDoAfter;
}

[DataDefinition]
public sealed partial class AutoSurgeonStep
{
    [DataField(required: true)]
    public LocId Label;

    [DataField(required: true)]
    public float Duration;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool Install;
}

[Serializable, NetSerializable]
public sealed partial class AutoSurgeonDoAfterEvent : SimpleDoAfterEvent;
