using Content.Shared.ADT.Vehicle.Components;
using Content.Shared.ADT.Vehicle.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using DrawDepthTag = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.ADT.Vehicle;

public sealed class ADTVehicleSystem : SharedADTVehicleSystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVehicleComponent, ComponentStartup>(OnVehicleStartup);
        SubscribeLocalEvent<ADTVehicleComponent, AppearanceChangeEvent>(OnVehicleAppearanceChange);
    }

    private void OnVehicleStartup(Entity<ADTVehicleComponent> ent, ref ComponentStartup args)
    {
        UpdateDrawDepth(ent);
    }

    protected override void OnVehicleRotated(Entity<ADTVehicleComponent> ent)
    {
        UpdateDrawDepth(ent);
    }

    private void UpdateDrawDepth(Entity<ADTVehicleComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var screenAngle = _transform.GetWorldRotation(ent) + _eyeManager.CurrentEye.Rotation;
        var over = screenAngle.GetCardinalDir() switch
        {
            Direction.North => ent.Comp.NorthOver,
            Direction.East => ent.Comp.EastOver,
            Direction.West => ent.Comp.WestOver,
            _ => ent.Comp.SouthOver,
        };

        var drawDepth = over ? DrawDepthTag.Doors : DrawDepthTag.WallMountedItems;
        _sprite.SetDrawDepth((ent, sprite), (int) drawDepth);
    }

    private void OnVehicleAppearanceChange(Entity<ADTVehicleComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !ent.Comp.AutoAnimate)
            return;

        if (Appearance.TryGetData<bool>(ent, ADTVehicleVisuals.AutoAnimate, out var autoAnimate, args.Component))
            _sprite.LayerSetAutoAnimated((ent, args.Sprite), VehicleVisualLayers.AutoAnimate, autoAnimate);
    }
}

public enum VehicleVisualLayers : byte
{
    AutoAnimate,
}
