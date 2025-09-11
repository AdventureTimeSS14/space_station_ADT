using Content.Server.Hands.Systems;
using Content.Server.UserInterface;
using Content.Shared.ADT.Storage.Components;
using Content.Shared.Hands.Components;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.ADT.Storage.System;

/// <summary>
/// Система, которая позволяет выдать выбор предмета на определенную сущность.
/// </summary>
public sealed class SuitStorageSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuitStorageComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SuitStorageComponent, SuitStorageMessage>(OnSelect);
    }

    /// <summary>
    ///  UI shit lmao
    /// </summary>
    private void OnGetVerbs(EntityUid uid, SuitStorageComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<ActorComponent>(args.User))
            return;

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("suitstorage-select-open-ui"),
            Act = () =>
            {
                _ui.TryOpenUi(uid, SuitStorageUiKey.Key, args.User);
            }
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Выдача выбранного в ручки.
    /// </summary>
    private void OnSelect(Entity<SuitStorageComponent> ent, ref SuitStorageMessage args)
    {
        if (args.Index < 0 || args.Index >= ent.Comp.Options.Count)
            return;

        ent.Comp.Selected = args.Index;
        Dirty(ent);

        // Если руки будут заняты - предмет спаунит на саму сущность, так что всё ок.
        if (TryComp<ActorComponent>(args.Actor, out var actor) && actor.PlayerSession.AttachedEntity is { } user)
        {
            var coords = Transform(ent.Owner).Coordinates;
            var proto = ent.Comp.Options[args.Index];

            var spawned = Spawn(proto, Transform(user).Coordinates);

            if (!_hands.TryPickup(user, spawned))
            {
                Transform(spawned).Coordinates = coords;
            }

            RemComp<SuitStorageComponent>(ent.Owner); // Удаляем компонент после выдачи предмета, лучший костыль :D.
        }
    }
}