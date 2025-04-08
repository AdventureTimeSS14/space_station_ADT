
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.ShowEnergy;

[RegisterComponent]
public sealed partial class ShowEnergyAlarmComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> PowerAlert = "ModsuitPower";
    public EntityUid? User;
}
