using Content.Server.ADT.Mime;
using Content.Shared.ADT.Mime;
using Content.Server.Popups;
using Robust.Shared.Random;
using Content.Server.Actions;

public sealed class MimeBaloonSystem : EntitySystem
{

    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimeBaloonComponent, SpawnBaloonEvent>(OnSpawnBaloonMime);
        SubscribeLocalEvent<MimeBaloonComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MimeBaloonComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(EntityUid uid, MimeBaloonComponent component, ComponentInit args)
    {
        _action.AddAction(uid, ref component.Action, "ADTActionBaloonOfMime");
    }

    private void OnRemove(EntityUid uid, MimeBaloonComponent component, ComponentRemove args)
    {
        _action.RemoveAction(uid, component.Action);
    }

    private void OnSpawnBaloonMime(EntityUid uid, MimeBaloonComponent component, SpawnBaloonEvent args)
    {

        var xform = Transform(args.Performer);

        _popupSystem.PopupEntity(Loc.GetString("mime-baloon-popup", ("entity", uid)), uid);

        var randomPickPrototype = _random.Pick(component.ListPrototypesBaloon);

        Spawn(randomPickPrototype, xform.Coordinates);
        _action.StartUseDelay(component.Action);
    }
}
