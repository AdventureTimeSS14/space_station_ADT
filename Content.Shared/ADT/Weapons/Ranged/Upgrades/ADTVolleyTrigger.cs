namespace Content.Shared.ADT.Weapons.Ranged.Upgrades;

public sealed class ADTVolleyTrigger
{
    public bool Used;
    public bool TryUse()
    {
        if (Used)
            return false;

        Used = true;
        return true;
    }
}
