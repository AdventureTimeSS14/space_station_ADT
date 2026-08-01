using System.Linq;
using Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Weapons.Ranged.Upgrades;

public sealed class ADTGunUpgradeVisualsSystem : VisualizerSystem<ADTUpgradeableGunComponent>
{
    private const int GunLayer = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTProjectileTracerComponent, ComponentStartup>(OnTracerStartup);
        SubscribeLocalEvent<ADTProjectileTracerComponent, AfterAutoHandleStateEvent>(OnTracerState);
    }

    protected override void OnAppearanceChange(EntityUid uid, ADTUpgradeableGunComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || args.Sprite.AllLayers.Count() <= GunLayer)
            return;

        if (!AppearanceSystem.TryGetData<Color>(uid, ADTGunChassisVisuals.Color, out var color, args.Component))
            color = Color.White;

        SpriteSystem.SetColor((uid, args.Sprite), color);

        if (component.StockSprite is not { } stock)
            return;

        AppearanceSystem.TryGetData<string>(uid, ADTGunChassisVisuals.Chassis, out var chassis, args.Component);

        var sprite = chassis != null && component.ChassisSprites.TryGetValue(chassis, out var chassisSprite)
            ? chassisSprite
            : stock;

        SpriteSystem.LayerSetSprite((uid, args.Sprite), GunLayer, sprite);
    }

    private void OnTracerStartup(Entity<ADTProjectileTracerComponent> ent, ref ComponentStartup args)
    {
        ApplyTracer(ent);
    }

    private void OnTracerState(Entity<ADTProjectileTracerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ApplyTracer(ent);
    }

    private void ApplyTracer(Entity<ADTProjectileTracerComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        SpriteSystem.SetColor((ent.Owner, sprite), ent.Comp.BoltColor);
    }
}
