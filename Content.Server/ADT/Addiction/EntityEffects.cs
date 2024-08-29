using Content.Shared.ADT.Addiction.AddictedComponent;
using Content.Shared.StatusEffect;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
namespace Content.Server.ADT.Addiction.EntityEffects;
public sealed partial class AddictionEffect : EntityEffect
{
/*    [DataField(required: true)]
    public List<LocId> PopupMessages = new();*/


    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-addiction",("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs ev)
    {
        if (ev is not EntityEffectReagentArgs args)
            return;

/*        if (args.EntityManager.TryGetComponent<AddictedComponent>(args.TargetEntity, out var comp))
        {
            foreach (var item in PopupMessages)
            {
                if (comp.PopupMessages.Contains(item))
                    continue;
                comp.PopupMessages.Add(item);
            }
        }
        else
        {
            var comp1 = args.EntityManager.EnsureComponent<AddictedComponent>(args.TargetEntity);
            comp1.PopupMessages.AddRange(PopupMessages);
        }*/
    }
}
