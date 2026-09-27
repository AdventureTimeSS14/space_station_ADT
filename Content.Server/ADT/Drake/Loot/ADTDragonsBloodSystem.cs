using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.ADT.Drake.Loot;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.ADT.Drake.Loot;

public sealed class ADTDragonsBloodSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDragonsBloodComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<ADTDragonsBloodComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.Outcomes.Count == 0)
            return;

        var user = args.User;
        if (!HasComp<HumanoidProfileComponent>(user))
            return;

        args.Handled = true;

        var outcome = _random.Pick(ent.Comp.Outcomes);
        LocId? message = outcome.Message;

        if (outcome.MindAction is { } action)
        {
            var granted = TryGrantMindAction(user, action);

            if (granted == null)
                message = null;
            else if (granted == false)
                message = outcome.KnownMessage;
        }

        if (message is { } text)
            _popup.PopupEntity(Loc.GetString(text), user, user, PopupType.LargeCaution);

        _audio.PlayPvs(ent.Comp.DrinkSound, user, AudioParams.Default.WithVolume(_random.NextFloat(-20f, -6f)));

        EntityManager.AddComponents(user, outcome.AddComponents, false);
        _entityEffects.ApplyEffects(user, outcome.Effects, user: user);

        QueueDel(ent);
    }

    private bool? TryGrantMindAction(EntityUid user, string action)
    {
        if (!_mind.TryGetMind(user, out var mindId, out _))
            return null;

        var container = EnsureComp<ActionsContainerComponent>(mindId);

        foreach (var contained in container.Container.ContainedEntities)
        {
            if (MetaData(contained).EntityPrototype?.ID == action)
                return false;
        }

        if (_actionContainer.AddAction(mindId, action, container) is not { } actionId)
            return null;

        _actions.GrantContainedAction(user, (mindId, container), actionId);
        return true;
    }
}
