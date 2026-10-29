using Content.Shared.ADT.Revolutinary.Stash;
using Content.Shared.Thief;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.Revolitionary.Stash;

public sealed class ADTRevolutionKitSystem : EntitySystem
{
    [Dependency] private ADTRevolutionStashSystem _stash = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRevolutionKitComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<ADTRevolutionKitComponent, ThiefBackpackApproveMessage>(OnApprove);
        SubscribeLocalEvent<ADTRevolutionKitComponent, ThiefBackpackChangeSetMessage>(OnChangeSet);
    }

    private void OnUIOpened(Entity<ADTRevolutionKitComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUI(ent);
    }

    private void OnApprove(Entity<ADTRevolutionKitComponent> ent, ref ThiefBackpackApproveMessage args)
    {
        var user = args.Actor;

        if (!_stash.CanUse(user) || ent.Comp.SelectedSets.Count != ent.Comp.MaxSelectedSets)
            return;

        var coords = _transform.GetMapCoordinates(user);

        foreach (var i in ent.Comp.SelectedSets)
        {
            var set = ProtoMan.Index(ent.Comp.PossibleSets[i]);
            foreach (var item in set.Content)
            {
                var spawned = Spawn(item, coords);
                _transform.DropNextTo(spawned, user);
            }
        }

        _audio.PlayPvs(ent.Comp.ApproveSound, user);
        _ui.CloseUi(ent.Owner, ThiefBackpackUIKey.Key);

        RemComp<ADTRevolutionKitComponent>(ent);
    }

    private void OnChangeSet(Entity<ADTRevolutionKitComponent> ent, ref ThiefBackpackChangeSetMessage args)
    {
        if (!_stash.CanUse(args.Actor))
            return;

        if (args.SetNumber < 0 || args.SetNumber >= ent.Comp.PossibleSets.Count)
            return;

        if (!ent.Comp.SelectedSets.Remove(args.SetNumber))
            ent.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(ent);
    }

    private void UpdateUI(Entity<ADTRevolutionKitComponent> ent)
    {
        var data = new Dictionary<int, ThiefBackpackSetInfo>();

        for (var i = 0; i < ent.Comp.PossibleSets.Count; i++)
        {
            var set = ProtoMan.Index(ent.Comp.PossibleSets[i]);
            var info = new ThiefBackpackSetInfo(
                set.Name,
                set.Description,
                set.Sprite,
                ent.Comp.SelectedSets.Contains(i));
            data.Add(i, info);
        }

        _ui.SetUiState(ent.Owner,
            ThiefBackpackUIKey.Key,
            new ThiefBackpackBoundUserInterfaceState(data, ent.Comp.MaxSelectedSets, ent.Comp.ToolName, ent.Comp.ToolDesc));
    }
}
