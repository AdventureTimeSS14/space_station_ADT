using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

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
    private void AddADTTricksVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        if (_adminManager.HasAdminFlag(player, AdminFlags.VarEdit))
        {
            var scaleVerbName = Loc.GetString("admin-trick-scale-name");
            Verb scaleVerb = new()
            {
                Text = scaleVerbName,
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("ADT/Interface/Actions/actions_phantom.rsi/help-portal.png")),
                Act = () =>
                {
                    _quickDialog.OpenDialog(player, "Scale", "Scale",
                        (float scale) =>
                        {
                            _console.ExecuteCommand($"scale {args.Target.Id} {scale}");
                        }
                    );
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", scaleVerbName, Loc.GetString("admin-trick-scale-description")),
                Priority = -20
            };
            args.Verbs.Add(scaleVerb);
        }
    }
}
