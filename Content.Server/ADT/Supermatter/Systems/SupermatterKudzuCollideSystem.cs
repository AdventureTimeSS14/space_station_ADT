using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.RoundEnd;
using Content.Server.Examine;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Traits.Assorted;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Supermatter.Components;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.ADT.Supermatter;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Supermatter.Systems;

public sealed class SupermatterKudzuOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;



    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterKudzuCollideComponent, StartCollideEvent>(OnCollideEvent);
        SubscribeLocalEvent<SupermatterKudzuCollideComponent, InteractHandEvent>(OnHandInteract);
    }

    private void OnCollideEvent(EntityUid uid, SupermatterKudzuCollideComponent sm, ref StartCollideEvent args)
    {
        TryCollision(uid, sm, args.OtherEntity, args.OtherBody);
    }

    private void TryCollision(EntityUid uid, SupermatterKudzuCollideComponent sm, EntityUid target, PhysicsComponent? targetPhysics = null, bool checkStatic = true)
    {
        if (!Resolve(target, ref targetPhysics))
            return;

        if (targetPhysics.BodyType == BodyType.Static && checkStatic ||
            HasComp<SupermatterImmuneComponent>(target) ||
            HasComp<GodmodeComponent>(target) ||
            _container.IsEntityInContainer(uid))
            return;

        if (!HasComp<ProjectileComponent>(target))
        {
            if (HasComp<MobStateComponent>(target))
            {
                EntityManager.SpawnEntity(sm.CollisionResultPrototype, Transform(target).Coordinates);
            }

            var targetProto = MetaData(target).EntityPrototype;
            if (targetProto != null && targetProto.ID != sm.CollisionResultPrototype)
            {
                _popup.PopupEntity(Loc.GetString("supermatter-collide-mob", ("sm", uid), ("target", target)), uid, PopupType.LargeCaution);
                _audio.PlayPvs(sm.DustSound, uid);
            }
        }
    }

    private void OnHandInteract(EntityUid uid, SupermatterKudzuCollideComponent sm, ref InteractHandEvent args)
    {
        var target = args.User;

        if (HasComp<SupermatterImmuneComponent>(target) || HasComp<GodmodeComponent>(target))
            return;

        _popup.PopupEntity(Loc.GetString("supermatter-collide-mob", ("sm", uid), ("target", target)), uid, PopupType.LargeCaution);
        _audio.PlayPvs(sm.DustSound, uid);

        AddComp<SupermatterImmuneComponent>(target);

        EntityManager.SpawnEntity(sm.CollisionResultPrototype, Transform(target).Coordinates);
        EntityManager.QueueDeleteEntity(target);

        args.Handled = true;
    }
}