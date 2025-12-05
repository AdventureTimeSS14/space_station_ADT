// Inspired by Nyanotrasen
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.CharecterFlavor;
using Content.Shared.ADT.CharecterFlavor;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using System.Net.Http;
using System.Threading.Tasks;
using Robust.Shared.Timing;

namespace Content.Server.CharecterFlavor;

public sealed class CharecterFlavorSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    private static readonly HttpClient _httpClient = new HttpClient();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharecterFlavorComponent, GetVerbsEvent<ExamineVerb>>(OnOpenUi);

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        }
    }

    private void OnOpenUi(Entity<CharecterFlavorComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var detailsRange = _examine.IsInDetailsRange(args.User, ent);

        var user = args.User;
        var verb = new ExamineVerb{};

        if (ent.Comp.HeadshotUrl != string.Empty)
        {
            //тут получение если хэдшот есть
            verb = new ExamineVerb
            {
                Act = async () =>
                {
                    if (!TryComp<ActorComponent>(user, out var actor))
                        return;

                    var imageData = await DownloadImageAsync(ent.Comp.HeadshotUrl);

                    if (imageData != null)
                    {
                        ent.Comp.HeadshotBytes = imageData;
                        Dirty(user, ent.Comp);
                        _uiSystem.TryOpenUi(user, CharecterFlavorUiKey.Key, user);
                        //ообнуление, когда клиент точно получил т.е. через 3 секунды максимуцм
                        Timer.Spawn(3000, () =>
                        {
                            ent.Comp.HeadshotBytes = [];
                        });
                    }
                    else
                    {
                        Logger.Warning($"Failed to download headshot image for {user}");
                    }
                },
                Text = Loc.GetString("detail-examinable-verb-text"),
                Category = VerbCategory.Examine,
                Disabled = !detailsRange,
                Message = detailsRange ? null : Loc.GetString("detail-examinable-verb-disabled"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
            };
        }
        else
        {
            verb = new ExamineVerb
            {
                Act = () =>
                {
                    if (!TryComp<ActorComponent>(user, out var actor))
                        return;
                    Dirty(user, ent.Comp);
                    _uiSystem.TryOpenUi(user, CharecterFlavorUiKey.Key, user);
                    ent.Comp.HeadshotBytes = [];
                },
                Text = Loc.GetString("detail-examinable-verb-text"),
                Category = VerbCategory.Examine,
                Disabled = !detailsRange,
                Message = detailsRange ? null : Loc.GetString("detail-examinable-verb-disabled"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
            };
        }

        args.Verbs.Add(verb);
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