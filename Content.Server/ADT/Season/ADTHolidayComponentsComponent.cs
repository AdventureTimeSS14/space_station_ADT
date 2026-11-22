using Content.Server.Holiday;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Season;

[RegisterComponent, Access(typeof(ADTHolidaySystem))]
public sealed partial class ADTHolidayComponentsComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<HolidayPrototype>, ComponentRegistry> Holidays = new();
}
