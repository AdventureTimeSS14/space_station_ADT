using Content.Shared.ADT.Vehicle.Components;
using Content.Shared.ADT.Vehicle.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Vehicle;

public sealed class ADTVehicleSystem : SharedADTVehicleSystem
{
    [Dependency] private EyeSystem _eye = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVehicleRiderComponent, ComponentStartup>(OnRiderStartup);
        SubscribeLocalEvent<ADTVehicleRiderComponent, ComponentShutdown>(OnRiderShutdown);
        SubscribeLocalEvent<ADTVehicleRiderComponent, AfterAutoHandleStateEvent>(OnRiderHandleState);
        SubscribeLocalEvent<ADTVehicleComponent, AppearanceChangeEvent>(OnVehicleAppearanceChange);
    }

    private void OnRiderStartup(Entity<ADTVehicleRiderComponent> ent, ref ComponentStartup args)
    {
        if (TryComp(ent, out EyeComponent? eye))
            _eye.SetTarget(ent, eye.Target ?? ent.Comp.Vehicle, eye);
    }

    private void OnRiderShutdown(Entity<ADTVehicleRiderComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp(ent, out EyeComponent? eye))
            _eye.SetTarget(ent, null, eye);
    }

    private void OnRiderHandleState(Entity<ADTVehicleRiderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp(ent, out EyeComponent? eye) && (eye.Target == null || HasComp<ADTVehicleComponent>(eye.Target)))
            _eye.SetTarget(ent, ent.Comp.Vehicle, eye);
    }

    private void OnVehicleAppearanceChange(Entity<ADTVehicleComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (ent.Comp.HideRider
            && Appearance.TryGetData<bool>(ent, ADTVehicleVisuals.HideRider, out var hide, args.Component)
            && TryComp<SpriteComponent>(ent.Comp.LastRider, out var riderSprite))
        {
            _sprite.SetVisible((ent.Comp.LastRider.Value, riderSprite), !hide);
        }

        if (Appearance.TryGetData<int>(ent, ADTVehicleVisuals.DrawDepth, out var drawDepth, args.Component))
            _sprite.SetDrawDepth((ent, args.Sprite), drawDepth);

        if (ent.Comp.AutoAnimate
            && Appearance.TryGetData<bool>(ent, ADTVehicleVisuals.AutoAnimate, out var autoAnimate, args.Component))
        {
            _sprite.LayerSetAutoAnimated((ent, args.Sprite), VehicleVisualLayers.AutoAnimate, autoAnimate);
        }
    }
}

public enum VehicleVisualLayers : byte
{
    AutoAnimate,
}
