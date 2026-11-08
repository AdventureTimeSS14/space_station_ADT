using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Systems;

public sealed partial class ADTDishSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDishComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ADTDishComponent> ent, ref ExaminedEvent args)
    {
        if (!_proto.Resolve(ent.Comp.Quality, out var quality))
            return;

        args.PushMarkup(Loc.GetString("adt-dish-examine",
            ("method", Loc.GetString($"adt-cooking-method-{ent.Comp.Method.ToString().ToLowerInvariant()}")),
            ("quality", Loc.GetString(quality.Name)),
            ("color", quality.Color.ToHex())));
    }

    public void SetDish(EntityUid uid, ADTCookingMethod method, ProtoId<ADTDishQualityPrototype> quality)
    {
        var dish = EnsureComp<ADTDishComponent>(uid);
        dish.Method = method;
        dish.Quality = quality;
        Dirty(uid, dish);
    }

    public ADTDishQualityPrototype? GetQuality(Entity<ADTDishComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        return _proto.Resolve(ent.Comp.Quality, out var quality) ? quality : null;
    }

    public bool IsSauce(ProtoId<ReagentPrototype> reagent, ADTCookingSettingsPrototype settings)
    {
        if (settings.DressingReagents.Contains(reagent))
            return true;

        foreach (var group in settings.FlavorGroups)
        {
            if (group.Reagents.Contains(reagent))
                return true;
        }

        return false;
    }

    public Color? GetSauceMix(Solution? solution, ADTCookingSettingsPrototype settings, bool spice)
    {
        if (solution == null)
            return null;

        var total = 0f;
        var r = 0f;
        var g = 0f;
        var b = 0f;
        foreach (var reagent in solution.Contents)
        {
            var id = new ProtoId<ReagentPrototype>(reagent.Reagent.Prototype);
            if (!IsSauce(id, settings) || settings.SpiceReagents.Contains(id) != spice || !_proto.Resolve(id, out var proto))
                continue;

            var amount = reagent.Quantity.Float();
            r += proto.SubstanceColor.R * amount;
            g += proto.SubstanceColor.G * amount;
            b += proto.SubstanceColor.B * amount;
            total += amount;
        }

        if (total <= 0f)
            return null;

        return new Color(r / total, g / total, b / total);
    }

    public void AddSauce(EntityUid uid, Solution? source, ADTCookingSettingsPrototype settings)
    {
        if (!TryComp<ADTDishComponent>(uid, out var dish))
            return;

        var glaze = GetSauceMix(source, settings, false);
        var spice = GetSauceMix(source, settings, true);
        if (glaze == null && spice == null)
            return;

        if (glaze is { } glazeMix)
            dish.Glaze = Accumulate(dish.Glaze, glazeMix, settings.GlazeStrength, settings.GlazeMaxStrength);

        if (spice is { } spiceMix)
            dish.Spice = Accumulate(dish.Spice, spiceMix, settings.SpiceDensity, settings.SpiceMaxDensity);

        Dirty(uid, dish);
    }

    public bool HasSauce(Solution? solution, ADTCookingSettingsPrototype settings)
    {
        return GetSauceMix(solution, settings, false) != null || GetSauceMix(solution, settings, true) != null;
    }

    private static Color Accumulate(Color? old, Color mix, float step, float max)
    {
        if (old is not { } previous)
            return mix.WithAlpha(step);

        var color = Color.InterpolateBetween(previous.WithAlpha(1f), mix, 0.5f);
        return color.WithAlpha(Math.Min(max, previous.A + step * 0.5f));
    }
}
