// using Content.Shared.Administration;
// using Content.Shared.Database;
// using Content.Shared.Inventory;
// using Content.Shared.Verbs;
// using Robust.Shared.Map;
// using Robust.Shared.Player;
// using Robust.Shared.Utility;
// using System.Numerics;


// // ADT Content by Schrodinger71
// namespace Content.Server.Administration.Systems;

// public sealed partial class AdminVerbSystem
// {
//     private void AdminVariableTestArenaSystem(GetVerbsEvent<Verb> args)
//     {
//         if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
//             return;

//         var player = actor.PlayerSession;

//         if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
//             return;

//         if (_adminManager.IsAdmin(player))
//         {
//             if (HasComp<InventoryComponent>(args.Target))
//             {
//                 Verb sendToTestArenaAdt = new()
//                 {
//                     Text = "Send to test arena adt",
//                     Category = VerbCategory.Tricks,
//                     Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),

//                     Act = () =>
//                     {
//                         var (mapUid, gridUid) = _adminTestArenaSystem.AssertArenaLoaded(player);
//                         _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
//                     },
//                     Impact = LogImpact.Medium,
//                     Message = Loc.GetString("admin-trick-send-to-test-arena-description"),
//                     Priority = (int) TricksVerbPriorities.SendToTestArena,
//                 };
//                 args.Verbs.Add(sendToTestArenaAdt);
//             }
//         }
//     }
// }



