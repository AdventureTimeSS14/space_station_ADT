using System.Text;
using Content.Server.Discord;

namespace Content.Server.ADT.Discord.Bans.PayloadGenerators;

public sealed class DepartmentBanPayloadGenerator : BanPayloadGenerator
{
    public override WebhookPayload Generate(BanInfo info)
    {
        InitializeFooter(info);

        var username = Loc.GetString("discord-ban-department-ban-username");

        var embed = new WebhookEmbed
        {
            Color = 0xffea00,
            Description = GenerateBanDescription(info),
            Footer = Footer
        };

        return new WebhookPayload
        {
            Username = username,
            Embeds = new List<WebhookEmbed> { embed }
        };
    }

    private string GenerateBanDescription(BanInfo info)
    {
        var builder = new StringBuilder();

        var banHeader = Loc.GetString("discord-ban-department-ban-header", ("banId", info.BanId));
        var target = Loc.GetString("discord-ban-target", ("target", info.Target));
        var reason = Loc.GetString("discord-ban-reason", ("reason", info.Reason));

        var banDuration = TimeSpan.FromMinutes(info.Minutes);

        var duration = info.Minutes != 0
            ? Loc.GetString(
                "discord-ban-duration",
                ("days", banDuration.Days),
                ("hours", banDuration.Hours),
                ("minutes", banDuration.Minutes)
            )
            : Loc.GetString("discord-ban-permanent");

        var department = Loc.GetString("discord-ban-department-ban-department", ("department", info.AdditionalInfo["localizedDepartment"]));

        var expires = info.Minutes != 0
            ? Loc.GetString("discord-ban-unban-date", ("expires", info.Expires.ToString()!))
            : null;

        var player = info.Player is not null
            ? Loc.GetString("discord-ban-submitted-by", ("name", info.Player.Name))
            : Loc.GetString("discord-ban-submitted-by-system");

        builder.AppendLine(banHeader);
        builder.AppendLine(target);
        builder.AppendLine(reason);
        builder.AppendLine(duration);
        builder.AppendLine(department);

        if (expires is not null)
        {
            builder.AppendLine(expires);
        }

        builder.AppendLine(player);

        return builder.ToString();
    }
}
