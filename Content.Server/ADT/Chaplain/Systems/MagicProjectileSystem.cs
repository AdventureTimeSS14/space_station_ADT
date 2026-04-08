using Content.Shared.ADT.Chaplain.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Events;

namespace Content.Server.ADT.Chaplain.Systems;

/// <summary>
/// Система для магических снарядов. Проверяет иммунитет к магии у цели при попадании.
/// Если цель имеет иммунитет, снаряд поглощается без эффекта.
/// </summary>
public sealed class MagicProjectileSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagicProjectileComponent, StartCollideEvent>(OnMagicProjectileStartCollide);
    }

    private void OnMagicProjectileStartCollide(EntityUid uid, MagicProjectileComponent component, ref StartCollideEvent args)
    {
        if (!HasComp<MagicImmunityComponent>(args.OtherEntity))
            return;

        // Цель имеет иммунитет - отменяем столкновение
        // Note: StartCollideEvent - readonly struct, не имеет Cancelled
        // Просто удаляем снаряд, чтобы он не обрабатывался дальше

        // Удаляем снаряд
        QueueDel(uid);
    }
}
