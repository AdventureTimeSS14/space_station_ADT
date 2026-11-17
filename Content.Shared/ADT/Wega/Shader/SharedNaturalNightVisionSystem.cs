// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.Actions;

namespace Content.Shared.Shaders;

public sealed partial class SharedNaturalNightVisionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NaturalNightVisionComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NaturalNightVisionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Action = _action.AddAction(ent, ent.Comp.ActionProto);
        Dirty(ent.Owner, ent.Comp);
    }
}
