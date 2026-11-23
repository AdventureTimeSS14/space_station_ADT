using System.Numerics;
using Content.Shared.ADT.Cooking.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ADTCookwareComponent : Component
{
    public const string ContainerId = "adt_cookware";

    [DataField("cookwareType", required: true)]
    public ADTCookwareType Type = ADTCookwareType.Pan;

    [DataField]
    public string Solution = "cookware";

    [DataField]
    public int MaxItems = 4;

    [DataField]
    public int MaxAddons;

    [DataField]
    public List<Vector2> ContentOffsets = new();

    [DataField]
    public Vector2 ContentScale = new(0.5f, 0.5f);

    [DataField]
    public Vector2 ContentCenter;

    [DataField]
    public Vector2 ContentArea = new(0.5f, 0.4f);

    [DataField]
    public Vector2 StoveOffset;

    [DataField]
    public Vector2? StoveScale;

    [DataField]
    public string? MediumState;

    [DataField]
    public string? OvenState;

    [DataField]
    public string? OvenBatterState;

    [DataField]
    public Vector2 OvenContentArea = new(0.3125f, 0.0625f);

    [DataField]
    public int MediumLevels = 4;

    [ViewVariables]
    public Container Contents = default!;

    [DataField, AutoNetworkedField]
    public ADTCookingStage Stage = ADTCookingStage.Idle;

    [ViewVariables]
    public string? TargetKey;

    [ViewVariables]
    public ProtoId<ADTCookingRecipePrototype>? Recipe;

    [ViewVariables]
    public ADTCookingTargetKind TargetKind = ADTCookingTargetKind.None;

    [ViewVariables]
    public float CookTime;

    [ViewVariables]
    public float TargetTime;

    [ViewVariables]
    public float OvercookTime;

    [ViewVariables]
    public float BurnTime;

    [ViewVariables]
    public bool FlipDone;

    [ViewVariables]
    public bool FlipWindowOpen;

    [ViewVariables]
    public bool FlipAnnounced;

    [ViewVariables]
    public int StirsNeeded;

    [ViewVariables]
    public int StirsDone;

    [ViewVariables]
    public float NextStirAt;

    [ViewVariables]
    public float StirWindowEnd;

    [ViewVariables]
    public float SmokeTime;

    [ViewVariables]
    public TimeSpan? FireEndTime;

    [ViewVariables]
    public int Addons;

    [ViewVariables]
    public EntProtoId? SoupResult;

    [ViewVariables]
    public string? SoupName;

    [ViewVariables]
    public ProtoId<ADTDishQualityPrototype>? SoupQuality;

    [ViewVariables]
    public HashSet<string> SoupFlavors = new();

    [ViewVariables]
    public int SoupPortions;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan VisualRefTime;

    [ViewVariables, AutoNetworkedField]
    public float CookBase;

    [ViewVariables, AutoNetworkedField]
    public float CookRate;

    [ViewVariables, AutoNetworkedField]
    public float BurnBase;

    [ViewVariables, AutoNetworkedField]
    public float BurnRate;

    [ViewVariables, AutoNetworkedField]
    public Dictionary<NetEntity, EntProtoId> CookResults = new();

    [ViewVariables, AutoNetworkedField]
    public EntProtoId? CookCenterResult;

    [ViewVariables, AutoNetworkedField]
    public bool CookCenterLarge;

    [ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> LargeItems = new();

    [ViewVariables, AutoNetworkedField]
    public List<NetEntity> CookToppings = new();

    [ViewVariables, AutoNetworkedField]
    public bool CookToppingsStay;
}

public enum ADTCookingTargetKind : byte
{
    None,
    Recipe,
    Individual,
    Freeform,
}
