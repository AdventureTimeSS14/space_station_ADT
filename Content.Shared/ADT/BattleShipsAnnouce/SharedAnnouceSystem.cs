
using Robust.Shared.Audio;

namespace Content.Shared.ADT.BattleShipsAnnouce;

public abstract class SharedAnnounceSystem : EntitySystem
{
    public virtual void AnnounceKMT(string message, SoundSpecifier? sound = null)
    {
    }

    public virtual void AnnounceTSF(string message, SoundSpecifier? sound = null)
    {
    }


    public virtual void AnnounceBoth(string message, SoundSpecifier? sound = null)
    {
    }
}
