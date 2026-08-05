namespace Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

[RegisterComponent]
public sealed partial class XenobiologyEyePilotComponent : Component
{
    [DataField(required: true)]
    public EntityUid Console;

    [DataField(required: true)]
    public EntityUid Eye;

    [DataField]
    public EntityUid? CaptureSlimeAction;

    [DataField]
    public EntityUid? PlaceSlimeAction;

    [DataField]
    public EntityUid? FeedMonkeyAction;

    [DataField]
    public EntityUid? RecycleMonkeyAction;

    [DataField]
    public EntityUid? ReturnAction;
}
