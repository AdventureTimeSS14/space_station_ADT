using Content.Shared.Mobs;

namespace Content.Shared.ADT.Lavaland.LegionCore;

[RegisterComponent]
public sealed partial class ADTImplantedLegionCoreComponent : Component
{
    [DataField]
    public MobState TriggerState = MobState.Critical;
}
