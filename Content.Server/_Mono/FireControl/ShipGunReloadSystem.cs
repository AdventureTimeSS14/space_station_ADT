using Content.Shared._Mono.FireControl;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Mono.FireControl;

/// <summary>
/// Remembers when gunnery-controlled weapons fire and refreshes their consoles,
/// so the console can show each weapon's reload until it may fire again.
/// </summary>
public sealed partial class ShipGunReloadSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private FireControlSystem _fireControl = default!;

    /// <summary>
    /// Reloads shorter than this don't refresh the consoles, rapid-fire weapons would flood them with updates.
    /// </summary>
    public static readonly TimeSpan MinShownReload = TimeSpan.FromSeconds(0.3);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireControllableComponent, GunShotEvent>(OnShot);
    }

    private void OnShot(Entity<FireControllableComponent> ent, ref GunShotEvent args)
    {
        var now = _timing.CurTime;
        ent.Comp.LastShot = now;

        if (!TryComp<GunComponent>(ent, out var gun) || gun.NextFire - now < MinShownReload)
            return;

        if (!TryComp<FireControlServerComponent>(ent.Comp.ControllingServer, out var server))
            return;

        foreach (var console in server.Consoles)
        {
            if (TryComp<FireControlConsoleComponent>(console, out var consoleComp))
                _fireControl.QueueUiUpdate(console, consoleComp);
        }
    }
}
