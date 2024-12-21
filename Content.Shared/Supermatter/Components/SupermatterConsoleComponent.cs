using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;
using Content.Shared.Atmos;

namespace Content.Shared.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? SupermatterEntity = null;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string LinkingPort = "SupermatterSender";
}

[Serializable, NetSerializable]
public sealed class SupermatterConsoleUpdateState
(
    NetEntity? cristall,
    string procents,
    Dictionary<Gas, float> gases
)
    : BoundUserInterfaceState
{
    public NetEntity? Cristall = cristall;
    public string Procents = procents;
    public Dictionary<Gas, float> Gases = gases;
}

[Serializable, NetSerializable]
public enum SupermatterConsoleUiKey : byte
{
    Key
}