using Content.Shared.ADT.Dice.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Dice.Components;

/// <summary>
///     System that handles the Dice of Fate wizard objective.
///     Sets the objective title to "Вы не антагонист" and marks it as always complete.
/// </summary>
public sealed class DiceOfFateWizardObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiceOfFateWizardObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<DiceOfFateWizardObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAfterAssign(EntityUid uid, DiceOfFateWizardObjectiveComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("dice-of-fate-wizard-objective-name"), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("dice-of-fate-wizard-objective-desc"), args.Meta);
    }

    private void OnGetProgress(EntityUid uid, DiceOfFateWizardObjectiveComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 1f;
    }
}
