using System.Numerics;
using Content.Client.Administration.Systems;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;

namespace Content.Client.Administration;

internal sealed class AdminNameOverlay : Overlay
{
    private readonly AdminSystem _system;
    private readonly IEntityManager _entityManager;
    private readonly IEyeManager _eyeManager;
    private readonly EntityLookupSystem _entityLookup;
    private readonly IUserInterfaceManager _userInterfaceManager;
    private readonly Font _font;

    public AdminNameOverlay(AdminSystem system, IEntityManager entityManager, IEyeManager eyeManager, IResourceCache resourceCache, EntityLookupSystem entityLookup, IUserInterfaceManager userInterfaceManager)
    {
        _system = system;
        _entityManager = entityManager;
        _eyeManager = eyeManager;
        _entityLookup = entityLookup;
        _userInterfaceManager = userInterfaceManager;
        ZIndex = 200;
        _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.WorldAABB;

        foreach (var playerInfo in _system.PlayerList)
        {
            var entity = _entityManager.GetEntity(playerInfo.NetEntity);

            // Otherwise the entity can not exist yet
            if (entity == null || !_entityManager.EntityExists(entity))
            {
                continue;
            }

            // if not on the same map, continue
            if (_entityManager.GetComponent<TransformComponent>(entity.Value).MapID != args.MapId)
            {
                continue;
            }

            var aabb = _entityLookup.GetWorldAABB(entity.Value);

            // if not on screen, continue
            if (!aabb.Intersects(in viewport))
            {
                continue;
            }

            var uiScale = _userInterfaceManager.RootControl.UIScale;
            var lineoffset = new Vector2(0f, 11f) * uiScale;
            var screenCoordinates = _eyeManager.WorldToScreen(aabb.Center +
                                                              new Angle(-_eyeManager.CurrentEye.Rotation).RotateVec(
                                                                  aabb.TopRight - aabb.Center)) + new Vector2(1f, 7f);
            // if (playerInfo.Antag) // Я ЭТО КОМЕНЧУ ПОТОМУ ЧТО ТУТ НАПИСАНА ПОЕБОТА ПОЛНАЯ БЛЯТЬ, КТО ТАК делАеТ НАХУЙ
            // {
            //     args.ScreenHandle.DrawString(_font, screenCoordinates + (lineoffset * 2), "ANTAG", uiScale, Color.OrangeRed);
            // }
            // args.ScreenHandle.DrawString(_font, screenCoordinates+lineoffset, playerInfo.Username, uiScale, playerInfo.Connected ? Color.Yellow : Color.White);
            // args.ScreenHandle.DrawString(_font, screenCoordinates, playerInfo.CharacterName, uiScale, playerInfo.Connected ? Color.Aquamarine : Color.White);
            // ADT-STart-Tweak: Переписываю нормальный оверлей
            var currentOffset = Vector2.Zero;
            if (playerInfo.Antag)
            {
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, "ANTAG", uiScale, Color.OrangeRed);
                currentOffset += lineoffset;
            }
            args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.Username, uiScale, playerInfo.Connected ? Color.Yellow : Color.White);
            currentOffset += lineoffset;
            args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.CharacterName, uiScale, playerInfo.Connected ? Color.Aquamarine : Color.White);
            currentOffset += lineoffset;
            if (!string.IsNullOrEmpty(playerInfo.PlaytimeString))
            {
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.PlaytimeString, uiScale, playerInfo.Connected ? Color.Orange : Color.White);
                currentOffset += lineoffset;
            }
            if (playerInfo.Sponsor != null)
            {
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, "Sponsor", uiScale, playerInfo.Connected ? Color.Gold : Color.White);
                currentOffset += lineoffset;
            }
            // ADT-END
        }
    }
}
