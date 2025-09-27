namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(DynamicRandomRuleSystem))]
public sealed partial class DynamicRandomRuleComponent : Component
{
    /// <summary>
    /// The gamerules that get added by dynamic random.
    /// </summary>
    [DataField("additionalGameRules")]
    public HashSet<EntityUid> AdditionalGameRules = new();
}
