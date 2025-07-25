using Content.Server.ADT.Mime;
using Content.Shared.ADT.Mime;
using Content.Server.Popups;
using Robust.Shared.Random;

public sealed class MimeBaloonSystem : EntitySystem
{

    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimeBaloonComponent, SpawnBaloonEvent>(OnSpawnBaloonMime);
    }

    private void OnSpawnBaloonMime(EntityUid uid, MimeBaloonComponent component, SpawnBaloonEvent args)
    {

        var xform = Transform(args.Performer);

        _popupSystem.PopupEntity(Loc.GetString("mime-baloon-popup", ("entity", uid)), uid);

        var randomPickPrototype = _random.Pick(component.ListPrototypesBaloon);

        Spawn(randomPickPrototype, xform.Coordinates);
    }
}