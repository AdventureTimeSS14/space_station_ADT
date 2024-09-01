namespace Content.Server.ADT.Economy;

[RegisterComponent]
public sealed partial class BankCartridgeComponent : Component
{
    [ViewVariables]
    public int? AccountId;

    [ViewVariables]
    public EntityUid? Loader;

    public string AccountLinkResult = string.Empty;
}
