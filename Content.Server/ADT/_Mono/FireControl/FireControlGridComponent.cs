namespace Content.Server.ADT._Mono.FireControl;

[RegisterComponent]
public sealed partial class FireControlGridComponent : Component
{
    [ViewVariables]
    public EntityUid? ControllingServer = null;
}