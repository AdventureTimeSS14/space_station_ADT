using System.Linq;
using Content.Server.ADT.Body.Bones;
using Content.Shared.ADT.Body.Bones;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class MendFracturesEntityEffectSystem : EntityEffectSystem<ADTBonesComponent, MendFractures>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ADTFractureSystem _fracture = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<ADTBonesComponent> entity, ref EntityEffectEvent<MendFractures> args)
    {
        foreach (var (bone, _) in _fracture.EnumerateBones(entity).ToList())
        {
            if (_fracture.GetFractureType(bone.Comp) is not { } type)
                continue;

            if (type.Open && !args.Effect.MendOpen)
                continue;

            if (!_random.Prob(args.Effect.BoneChance))
                continue;

            _fracture.Mend(entity, bone);
            _popup.PopupEntity(Loc.GetString("adt-fracture-mended-reagent", ("limb", bone.Owner)), entity, entity, PopupType.Medium);
        }
    }
}
