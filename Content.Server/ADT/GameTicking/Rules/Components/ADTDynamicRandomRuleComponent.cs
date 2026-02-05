namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ADTDynamicRandomRuleSystem))]
public sealed partial class ADTDynamicRandomRuleComponent : Component
{
    /// <summary>
    /// The gamerules that get added by dynamic random.
    /// </summary>
    [DataField("additionalGameRules")]
    public HashSet<EntityUid> AdditionalGameRules = new();
}
