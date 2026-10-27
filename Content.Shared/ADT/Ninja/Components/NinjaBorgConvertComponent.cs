using Content.Shared.Silicons.Borgs;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Ninja.Components;

[RegisterComponent]
public sealed partial class NinjaBorgConvertComponent : Component
{
    /// <summary>
    /// Сколько раз можно конвертировать боргов. -1 = без ограничения.
    /// </summary>
    [DataField]
    public int Conversions = -1;

    [DataField]
    public Dictionary<ProtoId<BorgTypePrototype>, EntProtoId> ChassisByBorgType = new();

    [DataField]
    public List<EntProtoId> FallbackChassis = new();
}
