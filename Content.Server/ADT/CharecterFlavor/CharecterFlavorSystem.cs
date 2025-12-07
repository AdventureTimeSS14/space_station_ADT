// Inspired by Nyanotrasen
using Content.Shared.ADT.CharecterFlavor;
using Robust.Shared.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Content.Server.ADT.CharecterFlavor;

public sealed class CharecterFlavorSystem : SharedCharecterFlavorSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static HttpClient _httpClient = new HttpClient();

    public override void Initialize()
    {
        base.Initialize();

        var clientHandler = new HttpClientHandler()
        {
            Proxy = new WebProxy(_cfg.GetCVar<string>("ic.headshot_proxy")),
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _httpClient = new(clientHandler);

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
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
            return await _httpClient.GetByteArrayAsync(url);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to download image from {url}: {ex}");
            return null;
        }
    }
}
