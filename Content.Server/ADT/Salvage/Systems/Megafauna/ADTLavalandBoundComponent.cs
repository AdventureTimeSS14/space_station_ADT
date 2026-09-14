namespace Content.Server.ADT.Salvage.Systems;

[RegisterComponent]
public sealed partial class ADTLavalandBoundComponent : Component
{
    [ViewVariables]
    public EntityUid HomeMap = EntityUid.Invalid;
}
