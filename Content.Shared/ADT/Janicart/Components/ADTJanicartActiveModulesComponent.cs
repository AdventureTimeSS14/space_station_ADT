namespace Content.Shared.ADT.Janicart.Components;

[RegisterComponent]
[Access(typeof(SharedADTJanicartSystem))]
public sealed partial class ADTJanicartActiveModulesComponent : Component
{
    [DataField]
    public HashSet<EntityUid> ActiveModules = [];
}