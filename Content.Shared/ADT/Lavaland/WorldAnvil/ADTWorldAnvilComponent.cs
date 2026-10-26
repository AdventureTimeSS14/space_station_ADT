using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland.WorldAnvil;

[RegisterComponent]
public sealed partial class ADTWorldAnvilComponent : Component
{
    [DataField]
    public int Charges;

    [DataField]
    public List<ADTWorldAnvilFuel> Fuels = new();

    [DataField]
    public List<ADTWorldAnvilRecipe> Recipes = new();

    [ViewVariables]
    public EntityUid? Forging;

    [DataField]
    public SoundSpecifier ForgeStartSound = new SoundPathSpecifier("/Audio/ADT/Lavaland/anvil_start.ogg");

    [DataField]
    public SoundSpecifier ForgeEndSound = new SoundPathSpecifier("/Audio/ADT/Lavaland/anvil_end.ogg");
}

[DataDefinition]
public sealed partial class ADTWorldAnvilFuel
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public int Charges = 1;
}

[DataDefinition]
public sealed partial class ADTWorldAnvilRecipe
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public int Cost = 1;

    [DataField]
    public float Delay = 7f;

    [DataField]
    public SoundSpecifier? StartSound;

    [DataField]
    public LocId Message = "adt-world-anvil-forged";
}

[Serializable, NetSerializable]
public enum ADTWorldAnvilVisuals : byte
{
    Heated,
}
