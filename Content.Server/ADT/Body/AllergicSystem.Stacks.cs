using Content.Shared.ADT.Body.Allergies;

namespace Content.Server.ADT.Body;

public sealed partial class AllergicSystem : EntitySystem
{
    #region Events

    private void IncrementStackOnTrigger(EntityUid uid, AllergicComponent allergic, ref AllergyTriggeredEvent ev)
    {
        AdjustAllergyStack(uid, allergic.StackGrow, allergic);
    }

    #endregion

    #region Flow

    private void UpdateStack(EntityUid uid, AllergicComponent allergic)
    {
        if (allergic.AllergyStack <= 0)
            return;

        if (allergic.NextStackFade > _timing.CurTime)
            return;

        float newStack = allergic.AllergyStack + allergic.StackFade;
        SetAllergyStack(uid, newStack, allergic);

        if (newStack <= 0)
        {
            var faded = new AllergyFadedEvent();
            RaiseLocalEvent(uid, ref faded);
        }

        allergic.NextStackFade = allergic.NextStackFade + allergic.StackFadeRate;
    }

    #endregion

    #region API

    /// <summary>
    /// Метод для изменения стака аллергии на относительное значение.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="relativeStack"></param>
    /// <param name="allergic"></param>
    public void AdjustAllergyStack(EntityUid uid, float relativeStack, AllergicComponent? allergic = null)
    {
        if (!Resolve(uid, ref allergic))
            return;

        SetAllergyStack(uid, allergic.AllergyStack + relativeStack, allergic);
    }

    /// <summary>
    /// Метод для изменения стака аллергии.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="stack"></param>
    /// <param name="allergic"></param>
    public void SetAllergyStack(EntityUid uid, float stack, AllergicComponent? allergic = null)
    {
        if (!Resolve(uid, ref allergic))
            return;

        allergic.AllergyStack = MathF.Min(MathF.Max(0, stack), allergic.MaximumStack);
    }

    #endregion
}