using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Components.PickupHumans;

[RegisterComponent]
public sealed partial class TakenHumansComponent : Component
{
    public EntityUid Target = default;
}
