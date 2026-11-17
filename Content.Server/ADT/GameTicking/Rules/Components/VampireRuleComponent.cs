// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.FixedPoint;
using Content.Shared.Vampire;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="VampireRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(VampireRuleSystem))]
public sealed partial class VampireRuleComponent : Component
{
    [DataField]
    public Dictionary<EntityUid, VampireRoundInfo> VampiresInfo = new();
}

public sealed partial class VampireRoundInfo
{
    public string Name = string.Empty;
    public VampireClassEnum Class = VampireClassEnum.NonSelected;
    public FixedPoint2 TotalBloodDrank = FixedPoint2.Zero;
}
