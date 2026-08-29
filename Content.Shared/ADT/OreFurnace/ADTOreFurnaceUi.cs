using Content.Shared.ADT.OreFurnace.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.OreFurnace;

[Serializable, NetSerializable]
public enum ADTOreFurnaceUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ADTOreFurnaceSmeltMessage(ProtoId<OreSmeltRecipePrototype> recipe, int amount) : BoundUserInterfaceMessage
{
    public readonly ProtoId<OreSmeltRecipePrototype> Recipe = recipe;

    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class ADTOreFurnaceSmeltAllMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ADTOreFurnaceClaimPointsMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ADTOreFurnaceToggleSiloLinkMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ADTOreFurnaceFindSiloMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ADTOreFurnaceUpdateState(uint points, bool canClaim, bool siloLinkEnabled, string? siloName) : BoundUserInterfaceState
{
    public readonly uint Points = points;
    public readonly bool CanClaim = canClaim;
    public readonly bool SiloLinkEnabled = siloLinkEnabled;
    public readonly string? SiloName = siloName;
}
