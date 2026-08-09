using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Server.ADT.Construction.Systems;

public sealed class GrillMachinePartsSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityHeaterSystem _heater = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrillUpgradeComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<GrillUpgradeComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, GrillUpgradeComponent component, RefreshPartsEvent args)
    {
        var servoTier = args.GetPartRating(MachinePartIds.Servo, 1f);
        component.PowerMultiplier = servoTier;

        if (TryComp<EntityHeaterComponent>(uid, out var heater))
            _heater.SetPower(uid, component.BasePower * servoTier, heater);
    }

    private static void OnUpgradeExamine(EntityUid uid, GrillUpgradeComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-cook-speed", component.PowerMultiplier);
    }
}
