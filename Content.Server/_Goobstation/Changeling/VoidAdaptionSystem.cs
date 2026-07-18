using Content.Goobstation.Shared.Changeling.Components;
using Content.Goobstation.Shared.Changeling.Systems;
using Content.Server.Atmos.Components;
using Content.Server.ADT.Body;
using Content.Server.Polymorph.Systems;
using Content.Server.Temperature.Components;
using Content.Shared.Alert;
using Content.Shared.Polymorph;

namespace Content.Goobstation.Server.Changeling;

public sealed partial class VoidAdaptionSystem : SharedVoidAdaptionSystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private EntityQuery<ChangelingIdentityComponent> _lingQuery;

    public override void Initialize()
    {
        base.Initialize();

        _lingQuery = GetEntityQuery<ChangelingIdentityComponent>();

        SubscribeLocalEvent<VoidAdaptionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VoidAdaptionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VoidAdaptionComponent, GetTemperatureProtectionEvent>(OnGetTemperatureProtection);
        SubscribeLocalEvent<VoidAdaptionComponent, PolymorphedEvent>(OnPolymorphed);
    }

    private void OnStartup(Entity<VoidAdaptionComponent> ent, ref ComponentStartup args)
    {
        // Grant the void-survival immunities using the fork's own base immunity components,
        // so this works with the existing atmos/temperature/respiration systems.
        EnsureComp<PressureImmunityComponent>(ent);      // immunity to low/high pressure (barotrauma)
        EnsureComp<BreathingImmunityComponent>(ent);     // immunity to asphyxiation (no need to breathe)
        EnsureComp<TemperatureProtectionComponent>(ent); // temperature protection (coefficient forced to 0 below)

        _alerts.ShowAlert(ent.Owner, ent.Comp.Alert);
    }

    private void OnShutdown(Entity<VoidAdaptionComponent> ent, ref ComponentShutdown args)
    {
        // On entity deletion there is nothing to restore; only clean up for a living entity.
        if (TerminatingOrDeleted(ent))
            return;

        RemComp<PressureImmunityComponent>(ent);
        RemComp<BreathingImmunityComponent>(ent);
        RemComp<TemperatureProtectionComponent>(ent);

        _alerts.ClearAlert(ent.Owner, ent.Comp.Alert);
    }

    private void OnGetTemperatureProtection(Entity<VoidAdaptionComponent> ent, ref GetTemperatureProtectionEvent args)
    {
        // Full protection from environmental heating and cooling.
        args.Coefficient = 0f;
    }

    private void OnPolymorphed(Entity<VoidAdaptionComponent> ent, ref PolymorphedEvent args)
    {
        if (_lingQuery.TryComp(ent, out var ling)
            && ling.IsInLastResort)
            return;

        _polymorph.CopyPolymorphComponent<VoidAdaptionComponent>(ent, args.NewEntity);
    }
}
