using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.ShowEnergy;

[RegisterComponent]
public sealed partial class ShowEnergyAlarmComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> PowerAlert = "ModsuitPower";

    [DataField]
    public ProtoId<AlertPrototype>? NoBatteryAlert;

    public EntityUid? User;
}
