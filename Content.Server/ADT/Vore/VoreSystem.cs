using Content.Server.Fluids.EntitySystems;
using Content.Shared.ADT.Vore;
using Content.Shared.ADT.Vore.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Server.ADT.Vore;

public sealed class VoreSystem : SharedVoreSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoreComponent, VoreDoAfterEvent>(OnDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VoredComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var voredComp, out var damageable))
        {
            if (voredComp.Devourer == null || !Exists(voredComp.Devourer))
            {
                RemComp<VoredComponent>(uid);
                continue;
            }

            if (!_mobStateSystem.IsAlive(uid))
                continue;

            voredComp.AccumulatedTime += frameTime;

            if (voredComp.AccumulatedTime >= voredComp.DamageInterval)
            {
                voredComp.AccumulatedTime -= voredComp.DamageInterval;

                var damage = new DamageSpecifier();
                damage.DamageDict["Caustic"] = voredComp.AcidDamage;

                _damageableSystem.TryChangeDamage(uid, damage, true);
            }
        }
    }

    private void OnDoAfter(EntityUid uid, VoreComponent component, VoreDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (component.VoredEntities.Count >= component.MaxCapacity)
            return;

        if (!ContainerSystem.Insert(target, component.Stomach))
            return;

        var voredComp = EnsureComp<VoredComponent>(target);
        voredComp.Devourer = uid;

        component.VoredEntities.Add(target);

        EnsureComp<VoreOverlayComponent>(target);

        AudioSystem.PlayPvs(component.VoreSound, uid);

        UpdateReleaseActionState(uid, component);
        Dirty(uid, component);

        args.Handled = true;
    }

    public new void ReleaseEntity(EntityUid devourer, EntityUid target, VoreComponent component, bool silent = false)
    {
        var isDead = _mobStateSystem.IsDead(target);

        base.ReleaseEntity(devourer, target, component, silent);

        if (isDead)
        {
            SpawnGibs(devourer);
        }
    }

    private void SpawnGibs(EntityUid uid)
    {
        var xform = Transform(uid);

        var solution = new Solution();
        solution.AddReagent("Water", 20);

        _puddleSystem.TrySpillAt(xform.Coordinates, solution, out _);
    }
}

