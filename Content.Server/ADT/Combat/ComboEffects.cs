using Content.Server.Flash;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Combat;
using Content.Shared.StatusEffect;
using Content.Server.ADT.RespiratorBlocker;
using Content.Shared.Flash.Components;

namespace Content.Server.ADT.Combat;

///ВНИМАНИЕ!!!
///ЭТИ СИСТЕМЫ НЕ РАБОТАЮТ С ТЕКУЩИМ ВАРИАНТОМ КОМБО С ОДЕЖДОЙ
///ЧИНИТЕ, ЕСЛИ ХОТИТЕ, ЧТОБЫ РАБОТАЛО


/// <summary>
/// заставляет юзера кричать крутую фразу
/// </summary>
[Serializable]
public sealed partial class ComboSpeechEffect : IComboEffect
{
    [DataField]
    public string Speech;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var chat = entMan.System<ChatSystem>();

        chat.TrySendInGameICMessage(user, Loc.GetString(Speech), InGameICChatType.Speak, true, true, checkRadioPrefix: false);  //Speech that isn't sent to chat or adminlogs
    }
}
[Serializable]
public sealed partial class ComboBlockLungsEffect : IComboEffect
{
    [DataField]
    public int Time;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var status = entMan.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<BreathBlockComponent>(target, "BreathingBlocked", TimeSpan.FromSeconds(Time), false);
    }
}
