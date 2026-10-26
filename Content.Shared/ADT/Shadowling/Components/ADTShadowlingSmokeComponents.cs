using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingSmokeHealComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Heal = new();

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);
}

[RegisterComponent]
public sealed partial class ADTShadowlingSmokeHealedComponent : Component
{
    [ViewVariables]
    public TimeSpan NextHeal;
}
