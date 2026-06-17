using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.ADT.Shuttles;

/// <summary>
/// Interactive nav-map used by the drop pod console.
/// Supports panning and zooming; highlights the currently selected landing beacon.
/// </summary>
public sealed class DropPodNavMapControl : NavMapControl
{
    private Vector2? _selectedWorldPos;

    /// <summary>Centers the map view on the given world-space position on the station grid.</summary>
    public void CenterOnWorldPos(EntityUid gridUid, Vector2 worldCenter)
    {
        // CenterToCoordinates expects EntityCoordinates relative to the grid
        var xformSys = EntManager.System<SharedTransformSystem>();
        var localPos = Vector2.Transform(worldCenter, xformSys.GetInvWorldMatrix(gridUid));
        CenterToCoordinates(new EntityCoordinates(gridUid, localPos));
    }

    /// <summary>
    /// Highlights the beacon at the given world position with an orange crosshair.
    /// Pass null to clear the highlight.
    /// </summary>
    public void SetSelectedBeacon(Vector2? worldPos)
    {
        _selectedWorldPos = worldPos;
        TrackedCoordinates.Clear();

        if (worldPos == null || MapUid == null)
            return;

        // Also add to TrackedCoordinates so the base class draws the blinking dot
        var xformSys = EntManager.System<SharedTransformSystem>();
        var localPos = Vector2.Transform(worldPos.Value, xformSys.GetInvWorldMatrix(MapUid.Value));
        TrackedCoordinates[new EntityCoordinates(MapUid.Value, localPos)] = (true, Color.OrangeRed);
    }

    public DropPodNavMapControl() : base()
    {
        RectClipContent = true;
        // Map is view-only for click-selection (beacon selection is done via the list),
        // but dragging and scroll-zoom are enabled by the base class.
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (!_selectedWorldPos.HasValue || MapUid == null)
            return;

        // Convert world position to screen position using the same transform as NavMapControl.
        // Formula mirrors the TrackedCoordinates drawing in NavMapControl.Draw():
        //   local = InvWorldMatrix * worldPos - GetOffset()
        //   screen = ScalePosition(local.X, -local.Y)
        var xformSys = EntManager.System<SharedTransformSystem>();
        var local = Vector2.Transform(_selectedWorldPos.Value, xformSys.GetInvWorldMatrix(MapUid.Value)) - GetOffset();
        var screenPos = ScalePosition(new Vector2(local.X, -local.Y));

        var ring = Color.OrangeRed.WithAlpha(0.9f);

        handle.DrawCircle(screenPos, 10f, ring, filled: false);
        handle.DrawCircle(screenPos, 3f, ring, filled: true);

        const float arm = 16f;
        const float gap = 5f;
        handle.DrawLine(screenPos + new Vector2(-arm, 0), screenPos + new Vector2(-gap, 0), ring);
        handle.DrawLine(screenPos + new Vector2(gap, 0), screenPos + new Vector2(arm, 0), ring);
        handle.DrawLine(screenPos + new Vector2(0, -arm), screenPos + new Vector2(0, -gap), ring);
        handle.DrawLine(screenPos + new Vector2(0, gap), screenPos + new Vector2(0, arm), ring);
    }
}
