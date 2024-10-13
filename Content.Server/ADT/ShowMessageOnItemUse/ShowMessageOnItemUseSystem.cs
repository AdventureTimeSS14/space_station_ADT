using Content.Server.EUI;
using Content.Server.Mind;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Flash;
using Content.Shared.StatusEffect;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Server.Electrocution;
using Content.Shared.Movement.Components;
using Content.Server.Flash;

namespace Content.Server.ADT.ShowMessageOnItemUse;

public sealed partial class ShowMessageOnItemUseSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly FlashSystem _flashSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindFlushComponent, UseInHandEvent>(ItemUsed);
    }

    private void ItemUsed(EntityUid uid, MindFlushComponent component, UseInHandEvent args)
    {
        if (TryComp<LimitedChargesComponent>(uid, out var charges))
            if (_charges.IsEmpty(uid, charges))
                return;

        var transform = EntityManager.GetComponent<TransformComponent>(uid);

        var flashableQuery = GetEntityQuery<StatusEffectsComponent>();

        foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, component.Range))
        {
            if (!flashableQuery.TryGetComponent(entity, out var _))
                continue;
            if (entity == args.User)
                continue;
            if (TryComp<MindContainerComponent>(entity, out var mindContainer))
            {
                if (_mind.TryGetSession(mindContainer.Mind, out var session))
                {
                    _euiManager.OpenEui(new AdtAmnesiaEui(), session);
                    Console.WriteLine($"entity {entity} mind was flushed.");
                }
            }

            if (TryComp<InputMoverComponent>(entity, out var _))
            {
                _electrocutionSystem.TryDoElectrocution(entity, null, 10, TimeSpan.FromSeconds(15), refresh: true, ignoreInsulation: true);
            }


            if (TryComp<InputMoverComponent>(entity, out var _))
            {
                _flashSystem.FlashArea(entity, args.User, component.Range, component.FlashDuration, component.SlowTo, false);
            }

        }
    }
}

public sealed class AdtAmnesiaEui : BaseEui
{
}
