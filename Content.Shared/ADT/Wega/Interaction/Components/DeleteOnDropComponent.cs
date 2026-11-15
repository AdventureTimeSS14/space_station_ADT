// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class DeleteOnDropComponent : Component
    {
        [DataField("deleteOnDrop")]
        public bool DeleteOnDrop = true;
    }
}
