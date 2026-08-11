// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Effect that spawns an item and attempts to place it in the player's backpack.
/// If the player has no backpack or the item does not fit, it is spawned at their feet.
/// Server-side effect - handled by TraitSystem.
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
        // This effect needs to be applied server-side where we have access to
        // inventory and container systems. Handled by the server TraitSystem.
    }
}
