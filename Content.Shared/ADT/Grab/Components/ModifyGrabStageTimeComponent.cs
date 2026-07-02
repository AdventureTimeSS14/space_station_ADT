// SPDX-FileCopyrightText: 2025 ADT Team
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Grab;

/// <summary>
/// Worn armor that makes it take longer to advance grab stages against the wearer.
/// Modifiers are per-stage multipliers applied to the puller's StageChangeCooldown.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ModifyGrabStageTimeComponent : Component
{
    /// <summary>
    /// Multipliers per grab stage. Values > 1 mean it takes longer to reach that stage.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<GrabStage, float> Modifiers = new();
}
