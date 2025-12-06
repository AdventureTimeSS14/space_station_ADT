// Inspired by Nyanotrasen
using Content.Shared.ADT.CharecterFlavor;
using System.Net.Http;
using System.Threading.Tasks;

namespace Content.Server.ADT.CharecterFlavor;

public sealed class CharecterFlavorSystem : SharedCharecterFlavorSystem
{
    private static readonly HttpClient HttpClient = new HttpClient();

    public override void Initialize()
    {
        base.Initialize();

        if (HttpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        }
    }

    protected override async void OpenFlavor(EntityUid actor, EntityUid target)
    {
        base.OpenFlavor(actor, target);

        if (!TryComp<CharacterFlavorComponent>(target, out var flavor))
            return;

        if (flavor.HeadshotUrl == string.Empty)
            return;

        var image = await DownloadImageAsync(flavor.HeadshotUrl);

        if (image == null)
            return;

        var ev = new SetHeadshotUiMessage(GetNetEntity(target), image);
        RaiseNetworkEvent(ev, actor);
    }

    public async Task<byte[]?> DownloadImageAsync(string url)
    {
        try
        {
            return await HttpClient.GetByteArrayAsync(url);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to download image from {url}: {ex}");
            return null;
        }
    }
}
