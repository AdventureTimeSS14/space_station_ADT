using Robust.Shared.Serialization;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Combat;
namespace Content.Server.ADT.Combat;

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

        chat.TrySendInGameICMessage(user, Speech, InGameICChatType.Speak, true, true, checkRadioPrefix: false);  //Speech that isn't sent to chat or adminlogs
    }
}
