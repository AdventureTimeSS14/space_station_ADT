using System.Threading.Tasks;

namespace Content.Server.ADT.Discord.Bans;

public interface IDiscordBanInfoSender
{
    Task SendBanInfoAsync<TGenerator>(BanInfo info)
        where TGenerator : IDiscordBanPayloadGenerator, new();
}
