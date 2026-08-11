// SPDX-FileCopyrightText: 2026 ultradyper
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.SlimeBody;

/// <summary>
/// Marks a slime person whose body is made of a specific drink reagent
/// (chosen in the character editor). The reagent is used as blood and
/// shown in the examine text.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTSlimeBodyComponent : Component
{
    /// <summary>
    /// The reagent this slime person's body is made of.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "Slime";

    /// <summary>
    /// Next time the drink-to-blood transfer runs. Server-side only.
    /// </summary>
    [DataField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Cached stomach organ of the body. Server-side only.
    /// </summary>
    [DataField]
    public EntityUid? StomachOrgan;

    /// <summary>
    /// Next time the extra blood regeneration runs. Server-side only.
    /// </summary>
    [DataField]
    public TimeSpan BloodRegenNextUpdate;
}
