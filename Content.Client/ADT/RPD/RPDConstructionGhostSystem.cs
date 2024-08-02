using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.ADT.RPD;
using Content.Shared.ADT.RPD.Components;
using Content.Shared.ADT.RPD.Systems;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.ADT.RPD;

public sealed class RPDConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RPDSystem _rpdSystem = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;

    private string _placementMode = typeof(AlignRPDConstruction).Name;
    private Direction _placementDirection = default;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get current placer data
        var placerEntity = _placementManager.CurrentPermission?.MobUid;
        var placerProto = _placementManager.CurrentPermission?.EntityType;
        var placerIsRPD = HasComp<RPDComponent>(placerEntity);

        // Exit if erasing or the current placer is not an RPD (build mode is active)
        if (_placementManager.Eraser || (placerEntity != null && !placerIsRPD))
            return;

        // Determine if player is carrying an RPD in their active hand
        var player = _playerManager.LocalSession?.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands))
            return;

        var heldEntity = hands.ActiveHand?.HeldEntity;

        if (!TryComp<RPDComponent>(heldEntity, out var rpd))
        {
            // If the player was holding an RPD, but is no longer, cancel placement
            if (placerIsRPD)
                _placementManager.Clear();

            return;
        }

        // Update the direction the RPD prototype based on the placer direction
        if (_placementDirection != _placementManager.Direction)
        {
            _placementDirection = _placementManager.Direction;
            RaiseNetworkEvent(new RPDConstructionGhostRotationEvent(GetNetEntity(heldEntity.Value), _placementDirection));
        }

        // If the placer has not changed, exit
        _rpdSystem.UpdateCachedPrototype(heldEntity.Value, rpd);

        if (heldEntity == placerEntity && rpd.CachedPrototype.Prototype == placerProto)
            return;

        // Create a new placer
        var newObjInfo = new PlacementInformation
        {
            MobUid = heldEntity.Value,
            PlacementOption = _placementMode,
            EntityType = rpd.CachedPrototype.Prototype,
            Range = (int) Math.Ceiling(SharedInteractionSystem.InteractionRange),
            UseEditorContext = false,
        };

        _placementManager.Clear();
        _placementManager.BeginPlacing(newObjInfo);
    }
}
