using System.Threading;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(RevolutionaryCentComEventsSystem))]
public sealed partial class RevolutionaryCentComEventsComponent : Component
{
    [DataField]
    public List<EntProtoId> Events = new();

    [DataField]
    public TimeSpan FirstDelay = TimeSpan.FromMinutes(20);

    [DataField]
    public TimeSpan MinDelay = TimeSpan.FromMinutes(20);

    [DataField]
    public TimeSpan MaxDelay = TimeSpan.FromMinutes(30);

    [DataField]
    public int MaxEvents = 15;

    [DataField]
    public int EventsSent;

    public CancellationTokenSource? Token;
}
