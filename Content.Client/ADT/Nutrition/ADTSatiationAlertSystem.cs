using System.Numerics;
using Content.Client.Alerts;
using Content.Shared.ADT.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client.ADT.Nutrition;

public sealed class ADTSatiationAlertSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTSatiationBarComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
    }

    private void OnUpdateAlertSprite(Entity<ADTSatiationBarComponent> entity, ref UpdateAlertSpriteEvent args)
    {
        float fraction;
        var fat = false;
        if (args.Alert.ID == ADTSatiationBarComponent.HungerAlertId)
        {
            fraction = GetHungerFraction(args.ViewerEnt);
            fat = HasComp<ADTFatComponent>(args.ViewerEnt);
        }
        else if (args.Alert.ID == ADTSatiationBarComponent.ThirstAlertId)
        {
            fraction = GetThirstFraction(args.ViewerEnt);
        }
        else
        {
            return;
        }

        if (!_sprite.LayerMapTryGet(entity.Owner, ADTSatiationBarVisualLayers.Bar, out var layer, false))
            return;

        _sprite.LayerSetRsiState(entity.Owner, layer, $"{entity.Comp.BarStatePrefix}_{ToLevel(fraction, entity.Comp)}");
        _sprite.LayerSetColor(entity.Owner, layer, fat ? entity.Comp.FatColor : GetBarColor(entity.Comp, fraction));
        _sprite.LayerSetOffset(entity.Owner, layer, new Vector2(entity.Comp.BarOffsetX / EyeManager.PixelsPerMeter, 0));
    }

    private float GetHungerFraction(EntityUid uid)
    {
        if (!TryComp(uid, out HungerComponent? hunger))
            return 0f;

        var max = hunger.Thresholds.TryGetValue(HungerThreshold.Fat, out var fat)
            ? fat
            : hunger.Thresholds[HungerThreshold.Overfed];
        return _hunger.GetHunger(hunger) / max;
    }

    private float GetThirstFraction(EntityUid uid)
    {
        if (!TryComp(uid, out ThirstComponent? thirst))
            return 0f;

        return thirst.CurrentThirst / thirst.ThirstThresholds[ThirstThreshold.OverHydrated];
    }

    private static int ToLevel(float fraction, ADTSatiationBarComponent bar)
    {
        return (int) MathF.Round(Math.Clamp(fraction, 0f, 1f) * bar.MaxLevel);
    }

    private static Color GetBarColor(ADTSatiationBarComponent bar, float fraction)
    {
        if (fraction < bar.CriticalThreshold)
            return bar.CriticalColor;

        if (fraction < bar.LowThreshold)
            return bar.LowColor;

        if (fraction < bar.FullThreshold)
            return bar.MediumColor;

        return bar.FullColor;
    }
}