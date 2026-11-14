using Content.Server.Holiday;
using Content.Shared.ADT.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Season;

[RegisterComponent, Access(typeof(ADTHolidaySystem))]
public sealed partial class ADTHolidayVendingComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<HolidayPrototype>, ProtoId<VendingMachineInventoryPrototype>> Holidays = new();
}
