// SPDX-FileCopyrightText: 2026 ultradyper
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.FluidBody;

/// <summary>
/// Marks a creature whose body is made of a liquid other than blood
/// (slime people, diona, novakids, drasks). The health analyzer shows
/// fluid wording instead of blood wording for them.
/// </summary>
[RegisterComponent]
public sealed partial class ADTFluidBodyComponent : Component
{
}
