// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.BloodBrothers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodBrotherComponent : Component
{
    [DataField, AutoNetworkedField]
    public int TeamId;

    [DataField, AutoNetworkedField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodBrotherFaction";
}
