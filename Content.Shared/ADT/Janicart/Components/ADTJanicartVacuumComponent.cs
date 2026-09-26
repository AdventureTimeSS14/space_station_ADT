using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Janicart.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedADTJanicartSystem))]
public sealed partial class ADTJanicartVacuumComponent : Component
{
    [DataField]
    public float Range = 2f;

    [DataField]
    public ProtoId<TagPrototype> TrashTag = "Trash";

    [DataField]
    public ProtoId<TagPrototype> TrashBagTag = "TrashBag";

    [DataField]
    public string TrashBagSlot = "trashbag_slot";

    [DataField]
    public int MaxItemsPerCheck = 5;

    [DataField]
    public SoundSpecifier CollectSound = new SoundCollectionSpecifier("trashBagRustle");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextCheck = TimeSpan.Zero;

    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(0.5);
}