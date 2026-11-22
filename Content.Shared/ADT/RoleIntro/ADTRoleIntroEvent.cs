using Robust.Shared.Serialization;

namespace Content.Shared.ADT.RoleIntro;

[Serializable, NetSerializable]
public sealed class ADTRoleIntroEvent : EntityEventArgs
{
    public readonly string Title;
    public readonly string Text;
    public readonly TimeSpan LockTime;

    public ADTRoleIntroEvent(string title, string text, TimeSpan lockTime)
    {
        Title = title;
        Text = text;
        LockTime = lockTime;
    }
}
