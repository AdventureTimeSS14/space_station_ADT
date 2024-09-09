using Content.Shared.ADT.Addiction.AddictedComponent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Addiction.EntityEffects;
public sealed partial class AddictionEffect : EntityEffect
{
    [DataField(required: true)]
    public float TimeCoefficient = new(); // Коэфицент домножающий на себя время воздействие этого регаента на организм
    [DataField(required: true)]
    public float QuantityCoefficient = new(); // Коэфицент домножающий на себя количество усвоенного реагента организмом
    [DataField] public TimeSpan DelayTime = TimeSpan.FromSeconds(30); // Время между получением реагента


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
            if (comp.LastEffect + DelayTime >= IoCManager.Resolve<IGameTiming>().CurTime)
                comp.CurrentAddictedTime += (IoCManager.Resolve<IGameTiming>().CurTime - comp.LastEffect) * 2;
            else
                comp.CurrentAddictedTime += TimeSpan.FromSeconds(1) * TimeCoefficient * 2;
            comp.RemaningTime = comp.ChangeAddictionTypeTime;
            comp.TypeAddiction = 4;
            comp.LastEffect = IoCManager.Resolve<IGameTiming>().CurTime;
        }
    }
}
