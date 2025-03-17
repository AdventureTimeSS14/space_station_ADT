using System.Linq;
using Content.Server.Actions;
using Content.Server.ADT.Language;
using Content.Server.ADT.Salvage.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Shared.Access.Systems;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chasm;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Lathe;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

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
