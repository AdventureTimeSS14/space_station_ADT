namespace Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

[RegisterComponent]
public sealed partial class XenobiologyEyeComponent : Component
{
    [DataField(required: true)]
    public EntityUid Pilot;

    [DataField(required: true)]
    public EntityUid Console;
}
