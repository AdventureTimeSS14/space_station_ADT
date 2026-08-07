using Content.Shared.ADT.Crushers.Components;
using Content.Shared.ADT.Hierophant.Effects;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class SpawnHierophantChaserEffect : TrophyEffect
{
    [DataField]
    public EntProtoId ChaserProto = "ADTHierophantChaser";

    [DataField]
    public float Damage = 20f;

    [DataField]
    public float Speed = 0.3f;

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-vortex-talisman"));
    }

    public override void OnMarkDetonation(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityUid user,
        EntityUid target,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        var transform = entManager.System<SharedTransformSystem>();
        var chaser = entManager.SpawnEntity(ChaserProto, transform.GetMoverCoordinates(user));

        if (!entManager.TryGetComponent<HierophantChaserComponent>(chaser, out var comp))
            return;

        comp.Caster = user;
        comp.Target = target;
        comp.Speed = Speed;
        comp.Damage = Damage;
        comp.MonsterDamageBoost = false;
        comp.FriendlyFireCheck = true;
    }
}
