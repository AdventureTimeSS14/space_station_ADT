// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Vampire;

public sealed partial class HolyPointSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<EmotePrototype> Scream = "Scream";
    private static readonly float FireStackCount = 2.5f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var holyPointQuery = EntityQueryEnumerator<HolyPointComponent>();
        while (holyPointQuery.MoveNext(out var uid, out var holyPoint))
        {
            if (holyPoint.NextTimeTick <= 0)
            {
                holyPoint.NextTimeTick = 10;
                var vampires = _entityLookup.GetEntitiesInRange<VampireComponent>(Transform(uid).Coordinates, holyPoint.Range);
                foreach (var vampire in vampires)
                {
                    if (HasComp<SupremeVampireComponent>(vampire))
                        continue;

                    if (TryComp(vampire.Owner, out FlammableComponent? flammable))
                    {
                        flammable.FireStacks += FireStackCount;
                        _flammable.Ignite(vampire.Owner, uid);

                        _chat.TryEmoteWithoutChat(vampire, ProtoMan.Index(Scream), true);
                        _popup.PopupEntity(Loc.GetString("vampire-holy-point"), vampire.Owner, vampire.Owner, PopupType.LargeCaution);
                    }
                }
            }
            holyPoint.NextTimeTick -= frameTime;
        }
    }
}
