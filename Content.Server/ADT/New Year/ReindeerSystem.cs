// using Content.Server.Atmos.Components;
// using Content.Server.Atmos.Piping.EntitySystems;
// using Robust.Server.GameObjects;
// using Content.Shared.Atmos;
// using Content.Shared.Atmos.Components;
// using Content.Server.Atmos.EntitySystems;
// using Content.Server.Atmos.Piping.Components;
// using Content.Shared.Vehicle.Components;
// using Content.Shared.Containers.ItemSlots;

// namespace Content.Server.ADT.Balloon.System
// {
//     public sealed class BalloonSystem : EntitySystem
//     {
//         [Dependency]
//         private AppearanceSystem _appearance = default!;
//         [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
//         [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

//         public override void Initialize()
//         {
//             base.Initialize();
//             SubscribeLocalEvent<ItemSlotsComponent, AtmosDeviceUpdateEvent>(OnGasChanged);
//         }
//         public void ReleaseGas(Entity<GasTankComponent> gasTank)
//         {
//             // Проверяем наличие гелия
//             var adtgas = _atmosphereSystem.GetContainingMixture(gasTank.Owner, false, true);

//         }
//         public void OnGasChanged(EntityUid uid, ItemSlotsComponent component, ref AtmosDeviceUpdateEvent args)
//         {
//             var hasItem = _itemSlots.TryGetSlot(uid, "key_slot", out var itemSlot) && itemSlot.Item != null;
//             if (itemSlot == "Saddle")
//             {
//                 // Если гелий есть, устанавливаем спрайт взлёта шарика
//                 _appearance.SetData(uid, BalloonState.State, true);
//             }
//             else
//             {
//                 // В противном случае оставляем спрайт по умолчанию
//                 _appearance.SetData(uid, BalloonState.State, false);
//             }
//         }
//     }
// }