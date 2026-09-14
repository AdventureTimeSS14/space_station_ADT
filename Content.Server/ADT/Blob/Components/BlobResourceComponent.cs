// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Server.ADT.Blob.Components;

[RegisterComponent]
public sealed partial class BlobResourceComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("pointsPerPulsed")]
    public FixedPoint2 PointsPerPulsed = 3;
}
