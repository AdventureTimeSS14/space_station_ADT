using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Shared.Audio.Systems;
using Robust.Client.Player;
using Content.Shared.Overlays;

namespace Content.Shared.Mech;

/// <inheritdoc/>
public sealed class MechMedSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedMechComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<MedMechComponent, MechEjectPilotEvent>(OnEjectPilotEvent);
    }
    private void OnMechEntry(EntityUid uid, MedMechComponent component, MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;
        var player = _playerManager.LocalSession;
        var playerEntity = player?.AttachedEntity;
        if (playerEntity == null)
        {
            return;
        }
        if (!_entityManager.HasComponent<ShowHealthBarsComponent>(playerEntity))
        {
            var showHealthBarsComponent = new ShowHealthBarsComponent
            {
                DamageContainers = new List<string>
                {"Biological"},
                NetSyncEnabled = false
            };

            _entityManager.AddComponent(playerEntity.Value, showHealthBarsComponent, true);
        }
    }
    private void OnEjectPilotEvent(EntityUid uid, MedMechComponent component, MechEjectPilotEvent args)
    {
        var player = _playerManager.LocalSession;
        var playerEntity = player?.AttachedEntity;
        if (playerEntity == null)
        {
            return;
        }
        if (_entityManager.HasComponent<ShowHealthBarsComponent>(playerEntity))
        {
            _entityManager.RemoveComponent<ShowHealthBarsComponent>(playerEntity.Value);
        }
    }
}
