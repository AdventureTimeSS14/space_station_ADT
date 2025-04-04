using Content.Client.ADT.Fishing.Overlays;
using Content.Shared.ADT.Fishing.Components;
using Content.Shared.ADT.Fishing.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Fishing;

public sealed class FishingSystem : SharedFishingSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new FishingOverlay(EntityManager, _player));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<FishingOverlay>();
    }
    protected override void SetupFishingFloat(Entity<FishingRodComponent> fishingRod, EntityUid player, EntityCoordinates target) {}

    protected override void ThrowFishReward(EntProtoId fishId, EntityUid fishSpot, EntityUid target) {}

    protected override void CalculateFightingTimings(Entity<ActiveFisherComponent> fisher, ActiveFishingSpotComponent activeSpotComp) {}
}
