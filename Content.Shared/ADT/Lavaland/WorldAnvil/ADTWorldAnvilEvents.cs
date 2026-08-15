using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland.WorldAnvil;

[Serializable, NetSerializable]
public sealed partial class ADTWorldAnvilForgeDoAfterEvent : DoAfterEvent
{
    [DataField]
    public int RecipeIndex;

    public ADTWorldAnvilForgeDoAfterEvent()
    {
    }

    public ADTWorldAnvilForgeDoAfterEvent(int recipeIndex)
    {
        RecipeIndex = recipeIndex;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}

[Serializable, NetSerializable]
public sealed partial class ADTMagmiteUpgradeDoAfterEvent : SimpleDoAfterEvent;
