using Content.Shared.ADT.Stealth.Components;
using Content.Shared.Stealth.Components;
using Content.Shared.Tag;

namespace Content.Shared.Stealth;

public abstract partial class SharedStealthSystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    private void InitializeADT()
    {
        SubscribeLocalEvent<DigitalCamouflageComponent, CheckStealthWhitelistEvent>(CheckDigiCamo);
    }

    private void CheckDigiCamo(EntityUid uid, DigitalCamouflageComponent comp, ref CheckStealthWhitelistEvent args)
    {
        if (!args.User.HasValue)
            return;

        if (!_tag.HasTag(args.User.Value, "ADTSiliconStealthWhitelist"))
            args.Cancelled = true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="user"></param>
    /// <param name="stealthEnt"></param>
    /// <returns>True, если для <paramref name="user"/> отрисовывается шейдер</returns>
    public bool CheckStealthWhitelist(EntityUid? user, EntityUid stealthEnt)
    {
        var ev = new CheckStealthWhitelistEvent(user, stealthEnt);
        RaiseLocalEvent(stealthEnt, ref ev);
        return !ev.Cancelled;
    }

    public void SetDesc(EntityUid uid, string desc, StealthComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.ExaminedDesc = desc;

        Dirty(uid, component);
    }
}
