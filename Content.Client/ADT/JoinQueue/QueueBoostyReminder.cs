using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.ADT.JoinQueue;

public sealed partial class QueueBoostyReminder : IDisposable
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(20),
    ];

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private QueueBoostyWindow? _window;
    private TimeSpan _nextReminder;
    private int _shown;

    public QueueBoostyReminder()
    {
        IoCManager.InjectDependencies(this);
        _nextReminder = _timing.RealTime + Delays[0];
    }

    public void Update()
    {
        if (_timing.RealTime < _nextReminder)
            return;

        _shown++;
        _nextReminder = _timing.RealTime + Delays[Math.Min(_shown, Delays.Length - 1)];

        if (_window is { IsOpen: true })
            return;

        _window = new QueueBoostyWindow();
        _window.OpenCentered();
    }

    public void Dispose()
    {
        _window?.Close();
    }
}
