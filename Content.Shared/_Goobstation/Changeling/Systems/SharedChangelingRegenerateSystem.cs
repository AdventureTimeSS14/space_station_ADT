using Content.Goobstation.Shared.Changeling.Actions;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Content.Shared.Body.Systems; // ADT-Tweak
using Content.Shared.Body.Components; // ADT-Tweak

namespace Content.Goobstation.Shared.Changeling.Systems;

public abstract partial class SharedChangelingRegenerateSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _blood = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<BloodstreamComponent> _bloodQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingRegenerateComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingRegenerateComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ChangelingRegenerateComponent, ChangelingRegenerateEvent>(OnRegenerateAction);

        _bloodQuery = GetEntityQuery<BloodstreamComponent>();
    }

    private void OnMapInit(Entity<ChangelingRegenerateComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ActionEnt = _actions.AddAction(ent, ent.Comp.ActionId);
    }

    private void OnShutdown(Entity<ChangelingRegenerateComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);
    }

    private void OnRegenerateAction(Entity<ChangelingRegenerateComponent> ent, ref ChangelingRegenerateEvent args)
    {
        if (_bloodQuery.TryComp(ent, out var bloodComp))
        {
            _blood.TryModifyBleedAmount((ent, bloodComp), -bloodComp.BleedAmount);
            _blood.TryModifyBloodLevel((ent, bloodComp), bloodComp.BloodReferenceSolution.Volume);
        }

        RegenerateHeal(ent);

        _audio.PlayPredicted(ent.Comp.RegenSound, ent, ent);
        _popup.PopupClient(Loc.GetString(ent.Comp.RegenPopup), ent, ent);

        args.Handled = true;
    }

    // ADT-Tweak
    protected virtual void RegenerateHeal(Entity<ChangelingRegenerateComponent> ent) { }
}
