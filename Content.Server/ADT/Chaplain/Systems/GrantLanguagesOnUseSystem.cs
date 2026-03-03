using Content.Server.ADT.Chaplain.Components;
using Content.Server.ADT.Language;
using Content.Server.Popups;
using Content.Shared.ADT.Language;
using Content.Shared.Interaction.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Chaplain.Systems;

public sealed class GrantLanguagesOnUseSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantLanguagesOnUseComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, GrantLanguagesOnUseComponent comp, UseInHandEvent args)
    {
        var learned = new List<string>();

        foreach (var langId in comp.Languages)
        {
            if (_proto.TryIndex<LanguagePrototype>(langId, out var lang))
            {
                _language.AddSpokenLanguage(args.User, langId);
                learned.Add(Loc.GetString($"language-{lang.ID}-name"));
            }
        }

        _language.UpdateUi(args.User);

        if (learned.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("..."), args.User, args.User);
            return;
        }

        var languagesStr = string.Join(", ", learned);
        var key = learned.Count == 1 ? "grant-language-success" : "grant-languages-success";
        _popup.PopupEntity(Loc.GetString(key, ("languages", languagesStr)),
            args.User,
            args.User);

        QueueDel(uid);
    }
}
