using Content.Shared.ADT.Janicart;
using Content.Shared.ADT.Janicart.Components;
using Content.Client.Vehicle;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Janicart;

public sealed class ADTJanicartSystem : SharedADTJanicartSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const string BaseVehicleState = "vehicle";
    private const string BufferVehicleState = "vehicle_upgrade";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTJanicartUpgradeableComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnAppearance(Entity<ADTJanicartUpgradeableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var buffer = _appearance.TryGetData<bool>(ent, ADTJanicartUpgradeVisuals.Buffer, out var b, args.Component) && b;
        args.Sprite.LayerSetState(VehicleVisualLayers.AutoAnimate, buffer ? BufferVehicleState : BaseVehicleState);
    }
}