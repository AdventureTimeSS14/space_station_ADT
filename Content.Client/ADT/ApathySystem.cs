using Content.Shared.Drugs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Shared.Alert;
using Content.Client.UserInterface.Systems.DamageOverlays;
using Content.Shared.ADT;

namespace Content.Client.ADT;

public sealed class ApathySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    public static string ApathyKey = "Apathy";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ApathyComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ApathyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ApathyComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ApathyComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

    }

    private void OnInit(EntityUid uid, ApathyComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _alertsSystem.ShowAlert(uid, component.Alert);
    }

    private void OnShutdown(EntityUid uid, ApathyComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _alertsSystem.ClearAlert(uid, component.Alert);
    }

    private void OnPlayerAttached(EntityUid uid, ApathyComponent component, LocalPlayerAttachedEvent args)
    {
        _alertsSystem.ShowAlert(uid, component.Alert);
    }

    private void OnPlayerDetached(EntityUid uid, ApathyComponent component, LocalPlayerDetachedEvent args)
    {
        _alertsSystem.ClearAlert(uid, component.Alert);
    }
}
