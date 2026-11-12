using Content.Server.ADT.VendingMachines;
using Content.Server.Holiday;
using Content.Shared.ADT.VendingMachines;

namespace Content.Server.ADT.Season;

public sealed class ADTHolidaySystem : EntitySystem
{
    [Dependency] private HolidaySystem _holiday = default!;
    [Dependency] private VendingMachineSystem _vending = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTHolidayComponentsComponent, MapInitEvent>(OnComponentsMapInit);
        SubscribeLocalEvent<ADTHolidayVendingComponent, MapInitEvent>(OnVendingMapInit);
    }

    private void OnComponentsMapInit(Entity<ADTHolidayComponentsComponent> ent, ref MapInitEvent args)
    {
        foreach (var (holiday, registry) in ent.Comp.Holidays)
        {
            if (!_holiday.IsCurrentlyHoliday(holiday))
                continue;

            EntityManager.AddComponents(ent, registry);
        }
    }

    private void OnVendingMapInit(Entity<ADTHolidayVendingComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ADTVendingMachineComponent>(ent, out var vending))
            return;

        foreach (var (holiday, pack) in ent.Comp.Holidays)
        {
            if (!_holiday.IsCurrentlyHoliday(holiday))
                continue;

            _vending.AddInventoryFromPack(ent, pack, vending);
        }
    }
}
