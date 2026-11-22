using Content.Shared.ADT.Drawing;

namespace Content.Client.ADT.Drawing.Moderation;

public sealed class PaintingReportsSystem : EntitySystem
{
    public event Action<PaintingReportsStateEvent>? StateReceived;
    public event Action<string>? StatusReceived;
    public event Action? Changed;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PaintingReportsStateEvent>(ev => StateReceived?.Invoke(ev));
        SubscribeNetworkEvent<PaintingReportsStatusEvent>(ev => StatusReceived?.Invoke(ev.Status));
        SubscribeNetworkEvent<PaintingReportsChangedEvent>(_ => Changed?.Invoke());
    }

    public void Request(bool resolved)
    {
        RaiseNetworkEvent(new PaintingReportsRequestEvent
        {
            Resolved = resolved,
        });
    }

    public void Send(EntityEventArgs message)
    {
        RaiseNetworkEvent(message);
    }
}
