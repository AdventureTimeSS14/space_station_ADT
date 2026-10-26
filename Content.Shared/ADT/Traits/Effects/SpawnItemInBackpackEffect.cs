// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Spawns the item in the player's backpack, or at their feet if it doesn't fit.
/// </summary>
public sealed partial class SpawnItemInBackpackEffect : BaseTraitEffect
{
    /// <summary>
    /// The entity prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Item = string.Empty;

    public override void Apply(TraitEffectContext ctx)
    {
        // Server-side: needs inventory and container systems.
    }
}
