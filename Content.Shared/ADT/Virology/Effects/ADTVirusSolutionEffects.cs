using Content.Shared.EntityEffects;

namespace Content.Shared.ADT.Virology.Effects;

public sealed partial class ADTEvolveVirus : EntityEffectBase<ADTEvolveVirus>
{
    [DataField]
    public int LevelMin;

    [DataField]
    public int LevelMax = 2;
}

public sealed partial class ADTDevolveVirus : EntityEffectBase<ADTDevolveVirus>
{
}
