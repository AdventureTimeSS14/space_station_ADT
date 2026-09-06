using Content.Shared.ADT.Nutrition;
using Content.Shared.Alert;
using Content.Shared.Nutrition.Components;

namespace Content.Server.ADT.Nutrition;

public sealed class ADTSatiationAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HungerComponent, ComponentStartup>(OnHungerStartup);
        SubscribeLocalEvent<ThirstComponent, ComponentStartup>(OnThirstStartup);
    }

    private void OnHungerStartup(EntityUid uid, HungerComponent component, ComponentStartup args) => _alerts.ShowAlert(uid, ADTSatiationBarComponent.HungerAlertId);
    private void OnThirstStartup(EntityUid uid, ThirstComponent component, ComponentStartup args) => _alerts.ShowAlert(uid, ADTSatiationBarComponent.ThirstAlertId);
}