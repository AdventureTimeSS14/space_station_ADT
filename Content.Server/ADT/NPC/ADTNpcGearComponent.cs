namespace Content.Server.ADT.NPC;

[RegisterComponent, Access(typeof(ADTNpcGearSystem))]
public sealed partial class ADTNpcGearComponent : Component
{
    [ViewVariables]
    public readonly List<ADTNpcHeldItem> Held = new();

    [ViewVariables]
    public readonly List<ADTNpcReserveItem> Reserve = new();

    [ViewVariables]
    public bool Dirty = true;

    [ViewVariables]
    public TimeSpan NextRefresh;

    [DataField]
    public TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public readonly Dictionary<ADTNpcWeaponRole, TimeSpan> NextWeaponSearch = new();

    [ViewVariables]
    public TimeSpan NextAnyWeaponSearch;
}

public readonly record struct ADTNpcHeldItem(string Hand, EntityUid Item, ADTNpcWeaponRole? Role);

public readonly record struct ADTNpcReserveItem(EntityUid Item, ADTNpcWeaponRole? Role);

public readonly record struct ADTNpcFoundWeapon(EntityUid Item, string? Hand);
