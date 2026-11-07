using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StarGazerHeldComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Holder;

    [DataField, AutoNetworkedField]
    public float EscapeChance = 0.10f;

    [DataField, AutoNetworkedField]
    public TimeSpan NextEscapeAttempt = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public TimeSpan EscapeCooldown = TimeSpan.FromSeconds(1.5f);

    [DataField, AutoNetworkedField]
    public bool AddedItemComponent = false;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StarGazerThrownComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Thrower;

    [DataField, AutoNetworkedField]
    public float WallDamage = 100f;

    [DataField, AutoNetworkedField]
    public float ArmorPenetration = 0.50f;

    [DataField, AutoNetworkedField]
    public bool HitWall = false;

    [DataField, AutoNetworkedField]
    public bool AddedItemComponent = false;
}
