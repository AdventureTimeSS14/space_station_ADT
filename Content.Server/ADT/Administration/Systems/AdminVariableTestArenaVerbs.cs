using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Numerics;

// ADT Content by Schrodinger71
namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly AdminTestArenaVariableSystem _adminTestArenaVariableSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private static readonly List<(string MapPath, string PrefixName)> AdminTestArenas = new()
    {
        ("/Maps/Test/admin_test_arena.yml", "SandBox"), // index: 0
        ("/Maps/Test/admin_test_arena.yml", "DevBox"),  // index: 1
    };

    private void AdminTestArenaVariableVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        if (!HasComp<InventoryComponent>(args.Target))
            return;

        Verb sendToTestArenaClassic = new()
        {
            Text = "Send to test arena classic sandbox",
            Category = VerbCategory.AdminRoom,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),

            Act = () =>
            {
                var (mapPath, prefixName) = AdminTestArenas[0]; // Получаем SandBox
                var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(player, mapPath, prefixName);
                _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-send-to-test-arena-description"),
            Priority = (int) TricksVerbPriorities.SendToTestArena,
        };
        args.Verbs.Add(sendToTestArenaClassic);

        Verb sendToTestArenaDevelop = new()
        {
            Text = "Send to test arena develop",
            Category = VerbCategory.AdminRoom,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),

            Act = () =>
            {
                var (mapPath, prefixName) = AdminTestArenas[1]; // Получаем DevBox
                var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(player, mapPath, prefixName);
                _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-send-to-test-arena-description"),
            Priority = (int) TricksVerbPriorities.SendToTestArena,
        };
        args.Verbs.Add(sendToTestArenaDevelop);
    }
}
