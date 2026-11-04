using Content.Shared.ADT.Surgery.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    public bool TryMatchToolQuality(ProtoId<SurgeryToolQualityPrototype> quality, EntityUid item, out float speed, out float success)
    {
        speed = 1f;
        success = 1f;

        if (!TryMatchQuality(quality, item, out var match))
            return false;

        speed = match.Speed;
        success = match.Success;
        return true;
    }
}
