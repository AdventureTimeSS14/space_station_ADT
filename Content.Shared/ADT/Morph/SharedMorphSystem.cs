using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.ADT.Morph;

public abstract class SharedMorphSystem : EntitySystem
{
    public override void Initialize()
    {
    }
}
[Serializable, NetSerializable]
public sealed partial class MorphDevourDoAfterEvent : SimpleDoAfterEvent { }
