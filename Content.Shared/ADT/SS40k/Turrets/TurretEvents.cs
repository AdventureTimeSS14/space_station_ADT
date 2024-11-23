using Content.Shared.Actions;

namespace Content.Shared.ADT.SS40k.Turrets;
public sealed partial class ControlReturnActionEvent : InstantActionEvent
{
}

public sealed class ReturnToBodyTurretEvent : EntityEventArgs
{
    public EntityUid TurretController;

    public ReturnToBodyTurretEvent(EntityUid turretcontroller)
    {
        TurretController = turretcontroller;
    }
}

public sealed class GettingControlledEvent : EntityEventArgs
{
    public EntityUid User;
    public EntityUid Controller;
    public GettingControlledEvent(EntityUid user, EntityUid controller)
    {
        User = user;
        Controller = controller;
    }
}
