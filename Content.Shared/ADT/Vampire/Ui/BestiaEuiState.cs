// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

[Serializable, NetSerializable]
public sealed partial class TrophiesEuiState : EuiStateBase
{
    public List<OrganDisplayInfo> Organs { get; }
    public List<PassiveBonusInfo> PassiveBonuses { get; }
    public List<AbilityDisplayInfo> Abilities { get; }

    public TrophiesEuiState(
        List<OrganDisplayInfo> organs,
        List<PassiveBonusInfo> passiveBonuses,
        List<AbilityDisplayInfo> abilities)
    {
        Organs = organs;
        PassiveBonuses = passiveBonuses;
        Abilities = abilities;
    }
}

[Serializable, NetSerializable]
public sealed partial class OrganDisplayInfo
{
    public BestiaOrganType Type { get; set; }
    public int Count { get; set; }
    public int MaxCount { get; set; }
    public Color CountColor { get; set; } = Color.White;
    public NetEntity? PreviewEntity { get; set; }
}

[Serializable, NetSerializable]
public sealed partial class PassiveBonusInfo
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public Color? ValueColor { get; set; }
    public bool IsMaxed { get; set; }
}

[Serializable, NetSerializable]
public sealed partial class AbilityDisplayInfo
{
    public NetEntity Action { get; set; }
    public string Name { get; set; } = "";
    public List<OrganBonusDetail> Bonuses { get; set; } = new();
}

[Serializable, NetSerializable]
public sealed partial class OrganBonusDetail
{
    public string OrganType { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsMaxed { get; set; }
}

[Serializable, NetSerializable]
public sealed partial class DissectSelectionEuiState : EuiStateBase
{
    public List<NetEntity> AvailableOrgans { get; }

    public DissectSelectionEuiState(List<NetEntity> availableOrgans)
    {
        AvailableOrgans = availableOrgans;
    }
}

[Serializable, NetSerializable]
public sealed partial class DissectOrganSelectedMessage : EuiMessageBase
{
    public NetEntity Target { get; }

    public DissectOrganSelectedMessage(NetEntity target)
    {
        Target = target;
    }
}
