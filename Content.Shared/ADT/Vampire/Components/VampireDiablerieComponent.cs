// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vampire.Components;

[Access(typeof(SharedVampireSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VampireDiablerieComponent : Component
{
    public static readonly EntProtoId SacramentInitiationActionPrototype = "ActionVampireSacramentInitiation";

    public EntityUid? SacramentInitiationActionEntity;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int DiablerieLevel = 0;

    [DataField]
    public int MaxDiablerieLevel = 4;

    [DataField]
    public float SuckingBonusPerLevel = 10f;

    [DataField]
    public float CooldownReductionPerLevel = 0.05f;
}
