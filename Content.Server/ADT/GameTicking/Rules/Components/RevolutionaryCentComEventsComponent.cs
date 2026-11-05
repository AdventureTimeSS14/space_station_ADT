using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(RevolutionaryCentComEventsSystem))]
public sealed partial class RevolutionaryCentComEventsComponent : Component
{
    [DataField]
    public EntProtoId EventPrototype = "ADTCentComRandomEventRevolution";

    [DataField]
    public TimeSpan FirstDelay = TimeSpan.FromMinutes(10);

    [DataField]
    public TimeSpan MinDelay = TimeSpan.FromMinutes(10);

    [DataField]
    public TimeSpan MaxDelay = TimeSpan.FromMinutes(20);

    [DataField]
    public TimeSpan NextEvent;

    [DataField]
    public int MaxEvents = 15;

    [DataField]
    public int EventsSent;
}
