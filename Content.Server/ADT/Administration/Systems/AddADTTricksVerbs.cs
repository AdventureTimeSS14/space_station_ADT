using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Sprite;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Toolshed;

/*
    ADT Content by 🐾 Schrödinger's Code 🐾
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    private SharedScaleVisualsSystem? _scaleVisuals;

    private void AddADTTricksVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        if (_adminManager.HasAdminFlag(player, AdminFlags.VarEdit))
        {
            _scaleVisuals ??= EntityManager.System<SharedScaleVisualsSystem>();
            var scaleVerbName = Loc.GetString("admin-trick-scale-name");

            Verb scaleVerb = new()
            {
                Text = scaleVerbName,
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("Interface/Actions/eyeopen.png")),
                Act = () =>
                {
                    _quickDialog.OpenDialog(player, "Scale", "Scale",
                        (float factor) =>
                        {
                            var newScale =
                                _scaleVisuals.GetSpriteScale(args.Target) * factor;

                            _scaleVisuals.SetSpriteScale(args.Target, newScale);
                        });
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ",
                    scaleVerbName,
                    Loc.GetString("admin-trick-scale-description")),
                Priority = -20
            };
            args.Verbs.Add(scaleVerb);
        }
    }
}
