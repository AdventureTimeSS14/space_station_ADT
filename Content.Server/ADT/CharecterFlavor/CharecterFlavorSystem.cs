// Inspired by Nyanotrasen
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.CharecterFlavor;
using Robust.Shared.Configuration;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.CharecterFlavor;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.ADT.CharecterFlavor;

public sealed class CharecterFlavorSystem : SharedCharecterFlavorSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    private static readonly HttpClient HttpClient = new();

    public override void Initialize()
    {
        base.Initialize();

        if (HttpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        }

        SubscribeNetworkEvent<RequestHeadshotPreviewEvent>(OnRequestHeadshotPreview);
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

    private async void OnRequestHeadshotPreview(RequestHeadshotPreviewEvent ev, EntitySessionEventArgs args)
    {
        if (!IsValidHeadshotUrl(ev.Url))
            return;

        var image = await DownloadImageAsync(ev.Url);
        if (image == null)
            return;

        RaiseNetworkEvent(new HeadshotPreviewEvent(image), Filter.SinglePlayer(args.SenderSession));
    }

    public async Task<byte[]?> DownloadImageAsync(string url)
    {
        try
        {
            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                return null;

            // лимит 5 MB
            const int maxSize = 5 * 1024 * 1024;

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var ms = new MemoryStream();

            var buffer = new byte[8192];
            int totalRead = 0;

            while (true)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0)
                    break;

                totalRead += read;
                if (totalRead > maxSize)
                    return null;

                ms.Write(buffer, 0, read);
            }

            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to download image from {url}: {ex}");
            return null;
        }
    }

    private bool IsValidHeadshotUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (url.Length > 500)
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // только http/https
        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var allowedDomain = _config.GetCVar(ADTCCVars.HeadshotDomain);

        if (!uri.Host.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.EndsWith("." + allowedDomain, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
