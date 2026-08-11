// SPDX-FileCopyrightText: 2026 ultradyper
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SlimeBody;

/// <summary>
/// A selectable body composition for slime people: the drink reagent their body is made of,
/// plus a sprite used in the character editor picker.
/// </summary>
public sealed record SlimeBodyComposition(
    string Id,
    LocId Name,
    ProtoId<ReagentPrototype> Reagent,
    string SpritePath,
    string SpriteState);

/// <summary>
/// The list of available slime body compositions.
/// </summary>
public static class SlimeBodyCompositions
{
    public static readonly IReadOnlyList<SlimeBodyComposition> All = new List<SlimeBodyComposition>
    {
        new("Cola", "adt-slime-body-composition-cola", "Cola", "Objects/Consumable/Drinks/cola.rsi", "icon"),
        new("GrapeSoda", "adt-slime-body-composition-grape-soda", "GrapeSoda", "Objects/Consumable/Drinks/gsodaglass.rsi", "icon"),
        new("OrangeLimeSoda", "adt-slime-body-composition-orange-lime-soda", "OrangeLimeSoda", "Objects/Consumable/Drinks/orangelime_soda.rsi", "icon"),
        new("AppleJuice", "adt-slime-body-composition-apple-juice", "JuiceApple", "Objects/Consumable/Food/produce.rsi", "apple"),
        new("OrangeJuice", "adt-slime-body-composition-orange-juice", "JuiceOrange", "Objects/Consumable/Drinks/orangejuice.rsi", "icon"),
        new("BananaJuice", "adt-slime-body-composition-banana-juice", "JuiceBanana", "Objects/Consumable/Drinks/banana.rsi", "icon"),
        new("GrapeJuice", "adt-slime-body-composition-grape-juice", "JuiceGrape", "Objects/Consumable/Drinks/grapejuice.rsi", "icon"),
        new("Beer", "adt-slime-body-composition-beer", "Beer", "Objects/Consumable/Drinks/beerglass.rsi", "icon"),
        new("Ale", "adt-slime-body-composition-ale", "Ale", "Objects/Consumable/Drinks/aleglass.rsi", "icon"),
        new("Whiskey", "adt-slime-body-composition-whiskey", "Whiskey", "Objects/Consumable/Drinks/whiskeyglass.rsi", "icon"),
        new("Vodka", "adt-slime-body-composition-vodka", "Vodka", "Objects/Consumable/Drinks/ginvodkaglass.rsi", "icon"),
        new("Rum", "adt-slime-body-composition-rum", "Rum", "Objects/Consumable/Drinks/rumglass.rsi", "icon"),
    };

    public static SlimeBodyComposition? Get(string? id)
    {
        if (id is null)
            return null;

        foreach (var composition in All)
        {
            if (composition.Id == id)
                return composition;
        }

        return null;
    }
}
