using Content.Shared.Actions;

namespace Content.Shared.Dragon;

public sealed partial class DragonDevourActionEvent : EntityTargetActionEvent
{
}

public sealed partial class DragonSpawnRiftActionEvent : InstantActionEvent
{
}


// ADT-Tweak-start
public sealed partial class DragonRoarActionEvent : InstantActionEvent
{
}

public sealed partial class DragonSpawnCarpHordeActionEvent : InstantActionEvent
{
}
// ADT-Tweak-end
