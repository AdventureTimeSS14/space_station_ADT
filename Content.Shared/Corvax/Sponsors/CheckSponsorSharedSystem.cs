using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared.Corvax.Sponsors
{
    [Serializable, NetSerializable]
    public sealed class CheckUserSponsor : EntityEventArgs
    {
        public string Player;
        public CheckUserSponsor(string p)
        {
            Player = p;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CheckedUserSponsor : EntityEventArgs
    {
        public bool IsSponsor;
        public CheckedUserSponsor(bool p)
        {
            IsSponsor = p;
        }
    }
}
