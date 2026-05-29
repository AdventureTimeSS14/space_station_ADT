using Content.Shared.ADT.ZombieJump;

namespace Content.Client.ADT.ZombieJump;
public sealed partial class ZombieJumpSystem : SharedZombieJumpSystem
{
    protected override void TryStunAndKnockdown(EntityUid uid, TimeSpan duration)
    {
        // На клиенте ничего не делаем
    }
}
