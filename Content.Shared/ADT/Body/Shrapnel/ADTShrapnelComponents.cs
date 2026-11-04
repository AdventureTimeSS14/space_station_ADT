using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Body.Shrapnel;

[RegisterComponent]
public sealed partial class ADTEmbeddableShrapnelComponent : Component
{
    [DataField]
    public float PainChance = 0.15f;

    [DataField]
    public DamageSpecifier PainDamage = new()
    {
        DamageDict = new()
        {
            { "Piercing", 4 },
        },
    };

    [DataField]
    public float FallChance = 0.01f;

    [DataField]
    public DamageSpecifier FallDamage = new()
    {
        DamageDict = new()
        {
            { "Piercing", 10 },
        },
    };

    [DataField]
    public DamageSpecifier ImpactDamage = new()
    {
        DamageDict = new()
        {
            { "Piercing", 4 },
        },
    };

    [DataField]
    public DamageSpecifier UnsafeRemovalDamage = new()
    {
        DamageDict = new()
        {
            { "Piercing", 16 },
        },
    };

    [DataField]
    public TimeSpan UnsafeRemovalTime = TimeSpan.FromSeconds(6);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTEmbeddedObjectsComponent : Component
{
    public const string ContainerId = "adt-embedded";
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ADTEmbeddedCarrierComponent : Component
{
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;
}

[RegisterComponent]
public sealed partial class ADTShrapnelOnHitComponent : Component
{
    [DataField]
    public EntProtoId Shrapnel = "ADTShrapnel";

    [DataField]
    public float EmbedChance = 0.7f;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, float> ZoneWeights = new()
    {
        ["Torso"] = 3f,
        ["Head"] = 1f,
        ["ArmLeft"] = 1f,
        ["ArmRight"] = 1f,
        ["HandLeft"] = 1f,
        ["HandRight"] = 1f,
        ["LegLeft"] = 1f,
        ["LegRight"] = 1f,
        ["FootLeft"] = 1f,
        ["FootRight"] = 1f,
    };
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTShrapnelOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public EntProtoId Shrapnel = "ADTShrapnel";

    [DataField]
    public float Range = 5f;

    [DataField]
    public int MaxShrapnel = 5;

    [DataField]
    public float EmbedChance = 0.7f;
}

[Serializable, NetSerializable]
public sealed partial class ADTShrapnelRemoveDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetEntity Organ;

    [DataField]
    public bool WithTool;

    private ADTShrapnelRemoveDoAfterEvent()
    {
    }

    public ADTShrapnelRemoveDoAfterEvent(NetEntity organ, bool withTool)
    {
        Organ = organ;
        WithTool = withTool;
    }

    public override DoAfterEvent Clone() => this;
}
