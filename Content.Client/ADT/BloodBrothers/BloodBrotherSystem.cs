// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using Content.Shared.ADT.BloodBrothers.Components;
using Content.Shared.Antag;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.BloodBrothers;

public sealed class BloodBrotherSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(Entity<BloodBrotherComponent> ent, ref GetStatusIconsEvent args)
    {
        var localPlayer = _player.LocalEntity;
        if (localPlayer == null)
            return;

        if (!_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            return;

        if (HasComp<ShowAntagIconsComponent>(localPlayer.Value))
        {
            args.StatusIcons.Add(iconPrototype);
            return;
        }

        if (!TryComp<BloodBrotherComponent>(localPlayer.Value, out var localBb))
            return;

        if (localBb.TeamId == ent.Comp.TeamId)
            args.StatusIcons.Add(iconPrototype);
    }
}
