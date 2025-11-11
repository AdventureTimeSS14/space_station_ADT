using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Weapons.Ranged;

public sealed class ModifyRecoilByTagSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModifyRecoilByTagComponent, SelfBeforeGunShotEvent>(OnBeforeShoot);
    }

    private void OnBeforeShoot(Entity<ModifyRecoilByTagComponent> ent, ref SelfBeforeGunShotEvent args)
    {
        foreach (var item in ent.Comp.Modifiers)
        {
            if (_tag.HasTag(args.Gun.Owner, item.Key))
                args.SpreadModifier *= item.Value;
        }
    }
}
