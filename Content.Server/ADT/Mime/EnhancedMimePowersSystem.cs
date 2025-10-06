using Content.Server.Abilities.Mime;
using Content.Server.Popups;

namespace Content.Server.ADT.Mime;

public sealed class EnhancedMimePowersSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnhancedMimePowersComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, EnhancedMimePowersComponent component, ComponentInit args)
    {
        if (!TryComp<MimePowersComponent>(uid, out var mimePowers))
            return;

        mimePowers.WallCount = 3;

        EnsureComp<MimeFingerGunComponent>(uid);
        EnsureComp<MimeThroatPunchComponent>(uid);

        _popupSystem.PopupEntity(Loc.GetString("mime-powers-enhanced"), uid, uid);
    }
}
