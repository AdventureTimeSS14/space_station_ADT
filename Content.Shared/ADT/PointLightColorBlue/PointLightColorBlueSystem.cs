using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Content.Shared.ADT.PointLightColorBlue;

public abstract class PointLightColorBlueSystem : EntitySystem
{
    public abstract PointLightColorBlueComponent EnsureLight(EntityUid uid);

    public abstract bool ResolveLight(EntityUid uid, [NotNullWhen(true)] ref PointLightColorBlueComponent? component);

    public abstract bool TryGetLight(EntityUid uid, [NotNullWhen(true)] out PointLightColorBlueComponent? component);

    public abstract bool RemoveLightDeferred(EntityUid uid);

    protected abstract void UpdatePriority(EntityUid uid, PointLightColorBlueComponent comp, MetaDataComponent meta);

    public void SetCastShadows(EntityUid uid, bool value, PointLightColorBlueComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || value == comp.CastShadows)
            return;

        comp.CastShadows = value;
        if (!Resolve(uid, ref meta))
            return;

        Dirty(uid, comp, meta);
        UpdatePriority(uid, comp, meta);
    }

    public void SetColor(EntityUid uid, Color value, PointLightColorBlueComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || value == comp.Color)
            return;

        comp.Color = value;
        Dirty(uid, comp);
    }

    public virtual void SetEnabled(EntityUid uid, bool enabled, PointLightColorBlueComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || enabled == comp.Enabled)
            return;

        comp.Enabled = enabled;
        RaiseLocalEvent(uid, new PointLightColorBlueEvent(comp.Enabled));
        if (!Resolve(uid, ref meta))
            return;

        Dirty(uid, comp, meta);
        UpdatePriority(uid, comp, meta);
    }

    public void SetEnergy(EntityUid uid, float value, PointLightColorBlueComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Energy, value))
            return;

        comp.Energy = value;
        Dirty(uid, comp);
    }

    public virtual void SetRadius(EntityUid uid, float radius, PointLightColorBlueComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Radius, radius))
            return;

        comp.Radius = radius;
        if (!Resolve(uid, ref meta))
            return;

        Dirty(uid, comp, meta);
        UpdatePriority(uid, comp, meta);
    }

    public void SetSoftness(EntityUid uid, float value, PointLightColorBlueComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Softness, value))
            return;

        comp.Softness = value;
        Dirty(uid, comp);
    }
}
