using Content.Shared.ADT.NaturalNightVision;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.ADT.NaturalNightVision;

/// <summary>
/// Включает и выключает PointLight лично для игрока.
/// </summary>
public sealed class NaturalNightVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NaturalNightVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NaturalNightVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NaturalNightVisionComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<NaturalNightVisionComponent, LocalPlayerDetachedEvent>(OnDetached);
    }

    private void OnStartup(EntityUid uid, NaturalNightVisionComponent component, ComponentStartup args)
    {
        if (uid == _player.LocalEntity)
            SetPersonalLight(uid, true);
    }

    private void OnShutdown(EntityUid uid, NaturalNightVisionComponent component, ComponentShutdown args)
    {
        SetPersonalLight(uid, false);
    }

    private void OnAttached(EntityUid uid, NaturalNightVisionComponent component, LocalPlayerAttachedEvent args)
    {
        SetPersonalLight(uid, true);
    }

    private void OnDetached(EntityUid uid, NaturalNightVisionComponent component, LocalPlayerDetachedEvent args)
    {
        SetPersonalLight(uid, false);
    }

    private void SetPersonalLight(EntityUid uid, bool enabled)
    {
        if (!_pointLight.TryGetLight(uid, out var light))
            return;

        _pointLight.SetEnabled(uid, enabled, light);
    }
}
