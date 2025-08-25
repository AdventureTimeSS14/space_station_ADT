using Content.Shared.Inventory;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Security.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CriminalRecordComponent : Component
{
    /// <summary>
    ///     The icon that should be displayed based on the criminal status of the entity.
    /// </summary>
// ADT-Beepsky-Start
    [DataField, AutoNetworkedField]
    public ProtoId<SecurityIconPrototype>? StatusIcon;

    [DataField, AutoNetworkedField]
    public SecurityStatus Status = SecurityStatus.None;

    /// <summary>
    ///     How naughty they have been :3
    ///     Certain stuff that is considered "being a criminal" will increase this while some may decreae it.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Points;

    [DataField]
    public Dictionary<SecurityStatus, float> SecurityStatusPoints = new()
    {
        {SecurityStatus.None, 0f},
        {SecurityStatus.Suspected, 5f},
        {SecurityStatus.Wanted, 10f},
        {SecurityStatus.Detained, 10f},
        {SecurityStatus.Paroled, 5f},
        {SecurityStatus.Discharged, 3f},
    };

    [DataField]
    public Dictionary<SlotFlags, float> ClothingSlotPoints = new()
    {
        {SlotFlags.HEAD, 0.75f},
        {SlotFlags.EYES, 0.25f},
        {SlotFlags.EARS, 0.125f},
        {SlotFlags.MASK, 0.125f},
        {SlotFlags.OUTERCLOTHING, 1f},
        {SlotFlags.INNERCLOTHING, 1f},
        {SlotFlags.NECK, 0.5f},
        {SlotFlags.BACK, 0.75f},
        {SlotFlags.BELT, 0.75f},
        {SlotFlags.GLOVES, 0.25f},
        {SlotFlags.FEET, 0.125f},
        {SlotFlags.SUITSTORAGE, 1f},
        {SlotFlags.UNDERWEART, 0.125f},
        {SlotFlags.UNDERWEARB, 0.125f},
        {SlotFlags.SOCKS, 0.125f},
    };
}

[Serializable, NetSerializable]
public sealed partial class GetCriminalPointsEvent : EntityEventArgs
{
    public float Points;

    public GetCriminalPointsEvent(float points)
    {
        Points = points;
    }
// ADT-Beepsky-End
}
