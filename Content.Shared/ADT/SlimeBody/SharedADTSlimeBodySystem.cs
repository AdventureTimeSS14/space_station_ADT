// SPDX-FileCopyrightText: 2026 ultradyper
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SlimeBody;

public abstract partial class SharedADTSlimeBodySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTSlimeBodyComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ADTSlimeBodyComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && _proto.TryIndex(component.Reagent, out var reagent))
        {
            args.PushMarkup(Loc.GetString("adt-slime-body-examine",
                ("reagent", reagent.LocalizedName),
                ("color", reagent.SubstanceColor.ToHexNoAlpha())));
        }
    }
}
