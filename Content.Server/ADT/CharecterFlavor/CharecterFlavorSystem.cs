// Inspired by Nyanotrasen
using Content.Shared.ADT.CharecterFlavor;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Content.Server.ADT.CharecterFlavor;

public sealed class CharecterFlavorSystem : SharedCharecterFlavorSystem
{
    private static HttpClient? _httpClient;

    private const string ProxyUrl = "http://proxy.example.com:8080";
    private const string ProxyUsername = "";
    private const string ProxyPassword = "";

    public override void Initialize()
    {
        base.Initialize();

        if (_httpClient == null)
        {
            var handler = new HttpClientHandler();

            // Настройка прокси если URL указан
            if (!string.IsNullOrEmpty(ProxyUrl))
            {
                if (string.IsNullOrEmpty(ProxyUsername))
                {
                    handler.Proxy = new WebProxy(ProxyUrl);
                }
                else
                {
                    handler.Proxy = new WebProxy
                    {
                        Address = new Uri(ProxyUrl),
                        Credentials = new NetworkCredential(ProxyUsername, ProxyPassword)
                    };
                }
                handler.ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            _httpClient = new HttpClient(handler);
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
            return await _httpClient!.GetByteArrayAsync(url);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to download image from {url}: {ex}");
            return null;
        }
    }
}