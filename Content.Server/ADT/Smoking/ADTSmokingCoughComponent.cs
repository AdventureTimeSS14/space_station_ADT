// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.ADT.Smoking;

[RegisterComponent]
public sealed partial class ADTSmokingCoughComponent : Component
{
    [DataField]
    public TimeSpan NextCough;
}
