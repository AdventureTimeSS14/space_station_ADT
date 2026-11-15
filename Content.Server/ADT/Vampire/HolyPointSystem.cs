// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Vampire.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Vampire;

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
