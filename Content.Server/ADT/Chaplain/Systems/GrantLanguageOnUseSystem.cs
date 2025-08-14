using Content.Server.ADT.Chaplain.Components;
using Content.Server.ADT.Language;
using Content.Server.Popups;
using Content.Shared.ADT.Language;
using Content.Shared.Interaction.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Chaplain.Systems;

public sealed class GrantLanguageOnUseSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantLanguageOnUseComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, GrantLanguageOnUseComponent comp, UseInHandEvent args)
    {
        if (!_proto.TryIndex<LanguagePrototype>(comp.Language, out var lang))
        {
            _popup.PopupEntity(Loc.GetString("..."), args.User, args.User);
            return;
        }

        _language.AddSpokenLanguage(args.User, comp.Language);
        _language.UpdateUi(args.User);
        _popup.PopupEntity(Loc.GetString("grant-language-success", ("language", lang.ID)),
            args.User,
            args.User);

        QueueDel(uid);
    }
}