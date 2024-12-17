using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;
using Content.Shared.Atmos;

namespace Content.Shared.Supermatter.Components;

[Serializable, NetSerializable]
public sealed class SupermatterUpdateState(
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

[RegisterComponent]
public sealed partial class SupermatterConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? SupermatterEntity = null;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string LinkingPort = "SupermatterSender";
}

[Serializable, NetSerializable]
public enum SupermatterConsoleUiKey : byte
{
    Key
}