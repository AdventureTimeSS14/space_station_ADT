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

    private static readonly List<(string MapPath, string PrefixName, string IconPath)> AdminTestArenas = new()
    {
        ("/Maps/Test/admin_test_arena.yml", "SandBox", "/Textures/Interface/VerbIcons/eject.svg.192dpi.png"), // index: 0
        ("/Maps/Test/admin_test_arena.yml", "DevBox", "/Textures/Interface/VerbIcons/eject.svg.192dpi.png"),  // index: 1
    };

    private void AdminTestArenaVariableVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        Verb sendToTestArenaClassic = new()
        {
            Text = "Send to test arena classic sandbox",
            Category = VerbCategory.AdminRoom,
            Icon = new SpriteSpecifier.Texture(new(AdminTestArenas[0].IconPath)),

            Act = () =>
            {
                var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(
                    player,
                    AdminTestArenas[0].MapPath,
                    AdminTestArenas[0].PrefixName
                );
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
            Icon = new SpriteSpecifier.Texture(new(AdminTestArenas[1].IconPath)),

            Act = () =>
            {
                var (mapUid, gridUid) = _adminTestArenaVariableSystem.AssertArenaLoaded(
                    player,
                    AdminTestArenas[1].MapPath,
                    AdminTestArenas[1].PrefixName
                );
                _transformSystem.SetCoordinates(args.Target, new EntityCoordinates(gridUid ?? mapUid, Vector2.One));
            },
            Impact = LogImpact.Medium,
            Message = Loc.GetString("admin-trick-send-to-test-arena-description"),
            Priority = (int) TricksVerbPriorities.SendToTestArena,
        };
        args.Verbs.Add(sendToTestArenaDevelop);
    }
}
