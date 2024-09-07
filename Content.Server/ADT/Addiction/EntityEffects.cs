using Content.Shared.ADT.Addiction.AddictedComponent;
/*using Content.Shared.StatusEffect;*/
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
/*using Content.Shared.Chemistry.Reagent;*/
/*using Robust.Shared.Prototypes;*/
//using Content.Shared.Body.Prototypes;

namespace Content.Server.ADT.Addiction.EntityEffects;
public sealed partial class AddictionEffect : EntityEffect
{
    /*    [DataField(required: true)]
        public List<LocId> PopupMessages = new();*/
    [DataField(required: true)]
    public float TimeCoefficient = new(); // Коэфицент домножающий на себя время воздействие этого регаента на организм
    [DataField(required: true)]
    public float QuantityCoefficient = new(); // Коэфицент домножающий на себя количество усвоенного реагента организмом
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TimeSpan _delayTime = TimeSpan.FromSeconds(1); // Время задержки эффекта
    //[Dependency] private readonly MetabolismGroupPrototype groupNarcotics = MetabolismGroupPrototype.ID; //
/*    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;*/

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-addiction", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs ev)
    {
        if (ev is not EntityEffectReagentArgs args)
            return;
        if (ev.EntityManager.TryGetComponent<AddictedComponent>(ev.TargetEntity, out var comp)) // Получили компонент того, на кого действует эффект.
        {
            if (_timing.CurTime < comp.LastEffect + _delayTime)
                return;

/*            if (!_prototypeManager.TryIndex<ReagentPrototype>(ev.TargetEntity.ToString, out var proto))
                return;

            if (!proto.Metabolisms.TryGetValue("Narcotic", out var entry))
                return;*/

            comp.CurrentAddictedTime += _delayTime;
            comp.RemaningTime = comp.ChangeAddictionTypeTime;
        }
        
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
