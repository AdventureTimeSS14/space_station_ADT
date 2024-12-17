using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server._TornadoTech.FriendlyFire;

public sealed partial class FriendlyFireSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FriendlyFireComponent, ProjectileHitAttemptEvent>(OnHitProjectileAttempt);
        SubscribeLocalEvent<FriendlyFireComponent, HitScanHitAttemptEvent>(OnHitHitScanAttempt);

        ToggleableInitialize();
    }

    private void OnHitProjectileAttempt(Entity<FriendlyFireComponent> friendlyFire, ref ProjectileHitAttemptEvent args)
    {
        if (HitAttempt(friendlyFire, args.Shooter))
            return;

        args.Cancel();
    }

    private void OnHitHitScanAttempt(Entity<FriendlyFireComponent> friendlyFire, ref HitScanHitAttemptEvent args)
    {
        if (HitAttempt(friendlyFire, args.Shooter))
            return;

        args.Cancel();
    }

    private bool HitAttempt(Entity<FriendlyFireComponent> friendlyFire, EntityUid? shooter)
    {
        return shooter is { } uid && HitAttempt(friendlyFire, uid);
    }

    private bool HitAttempt(Entity<FriendlyFireComponent> friendlyFire, EntityUid shooter)
    {
        if (!TryComp<FriendlyFireComponent>(shooter, out var shooterFriendlyFire))
            return true;

        if (!shooterFriendlyFire.Enabled)
            return true;

        if (!TryComp<NpcFactionMemberComponent>(shooter, out var factionShooter))
            return true;

        if (!TryComp<NpcFactionMemberComponent>(friendlyFire, out var faction))
            return true;

        if (!_faction.IsEntityFriendly((friendlyFire, faction), (shooter, factionShooter)))
            return true;

        return false;
    }

    public void Toggle(EntityUid uid)
    {
        if (!TryComp<FriendlyFireComponent>(uid, out var friendlyFire))
            return;

        Toggle((uid, friendlyFire));
    }

    public void Toggle(Entity<FriendlyFireComponent> friendlyFire)
    {
        SetState(friendlyFire, !friendlyFire.Comp.Enabled);
    }

    public void SetState(EntityUid uid, bool state)
    {
        if (!TryComp<FriendlyFireComponent>(uid, out var friendlyFire))
            return;

        SetState((uid, friendlyFire), state);
    }

    public void SetState(Entity<FriendlyFireComponent> friendlyFire, bool state)
    {
        friendlyFire.Comp.Enabled = state;
    }

    public bool GetState(EntityUid uid)
    {
        if (!TryComp<FriendlyFireComponent>(uid, out var friendlyFire))
            return false;

        return friendlyFire.Enabled;
    }

    public bool GetState(Entity<FriendlyFireComponent> friendlyFire)
    {
        return friendlyFire.Comp.Enabled;
    }
}
