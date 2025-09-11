using System.Linq;
using Content.Server.ADT.Language;
using Content.Server.ADT.Salvage.Components;
using Content.Server.Popups;
using Content.Shared.ADT.Language;
using Content.Shared.Interaction.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class GrantAllLanguagesOnUseSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantAllLanguagesOnUseComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, GrantAllLanguagesOnUseComponent comp, UseInHandEvent args)
    {
        var prototypes = _proto.EnumeratePrototypes<LanguagePrototype>().Where(x => x.Roundstart);

        foreach (var item in prototypes)
        {
            _language.AddSpokenLanguage(args.User, item.ID);
        }

        _language.UpdateUi(args.User);
        _popup.PopupEntity(Loc.GetString("popup-vavilon-book-used"), args.User, args.User);
        QueueDel(uid);
    }
}
