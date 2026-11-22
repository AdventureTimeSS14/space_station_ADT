using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Alert;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class ADTBorgJetpackAlertSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;

    public static readonly ProtoId<AlertPrototype> JetpackAlert = "ADTBorgJetpack";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBorgJetpackComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ADTBorgJetpackComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTBorgJetpackComponent, ADTBorgJetpackToggleAlertEvent>(OnAlertToggle);
    }

    private void OnStartup(Entity<ADTBorgJetpackComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<AlertsComponent>(ent.Owner);
        _alerts.ShowAlert(ent.Owner, JetpackAlert);
    }

    private void OnShutdown(Entity<ADTBorgJetpackComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent.Owner, JetpackAlert);
    }

    private void OnAlertToggle(Entity<ADTBorgJetpackComponent> ent, ref ADTBorgJetpackToggleAlertEvent args)
    {
        args.Handled = true;
        RaiseLocalEvent(ent.Owner, new ToggleJetpackEvent { Performer = ent.Owner });
    }
}
