// using System.Diagnostics.CodeAnalysis;
// using Robust.Shared.Maths;

// namespace Content.Shared.ADT.PointLightColorBlue;

// public sealed class PointLightColorBlueSystem : EntitySystem
// {
//     public partial PointLightColorBlueComponent EnsureLight(EntityUid uid);

//     public partial bool ResolveLight(EntityUid uid, [NotNullWhen(true)] ref PointLightColorBlueComponent? component);

//     public partial bool TryGetLight(EntityUid uid, [NotNullWhen(true)] out PointLightColorBlueComponent? component);

//     public partial bool RemoveLightDeferred(EntityUid uid);

//     protected partial void UpdatePriority(EntityUid uid, PointLightColorBlueComponent comp, MetaDataComponent meta);

//     public void SetCastShadows(EntityUid uid, bool value, PointLightColorBlueComponent? comp = null, MetaDataComponent? meta = null)
//     {
//         if (!ResolveLight(uid, ref comp) || value == comp.CastShadows)
//             return;

//         comp.CastShadows = value;
//         if (!Resolve(uid, ref meta))
//             return;

//         Dirty(uid, comp, meta);
//         UpdatePriority(uid, comp, meta);
//     }

//     public void SetColor(EntityUid uid, Color value, PointLightColorBlueComponent? comp = null)
//     {
//         if (!ResolveLight(uid, ref comp) || value == comp.Color)
//             return;

//         comp.Color = value;
//         Dirty(uid, comp);
//     }

//     public void SetEnabled(EntityUid uid, bool enabled, PointLightColorBlueComponent? comp = null, MetaDataComponent? meta = null)
//     {
//         if (!ResolveLight(uid, ref comp) || enabled == comp.Enabled)
//             return;

//         comp.Enabled = enabled;
//         RaiseLocalEvent(uid, new PointLightColorBlueEvent(comp.Enabled));
//         if (!Resolve(uid, ref meta))
//             return;

//         Dirty(uid, comp, meta);
//         UpdatePriority(uid, comp, meta);
//     }

//     public void SetEnergy(EntityUid uid, float value, PointLightColorBlueComponent? comp = null)
//     {
//         if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Energy, value))
//             return;

//         comp.Energy = value;
//         Dirty(uid, comp);
//     }

//     public void SetRadius(EntityUid uid, float radius, PointLightColorBlueComponent? comp = null, MetaDataComponent? meta = null)
//     {
//         if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Radius, radius))
//             return;

//         comp.Radius = radius;
//         if (!Resolve(uid, ref meta))
//             return;

//         Dirty(uid, comp, meta);
//         UpdatePriority(uid, comp, meta);
//     }

//     public void SetSoftness(EntityUid uid, float value, PointLightColorBlueComponent? comp = null)
//     {
//         if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Softness, value))
//             return;

//         comp.Softness = value;
//         Dirty(uid, comp);
//     }

//     [Dependency] private readonly MetaDataSystem _metadata = default!;

//     public override void Initialize()
//     {
//         base.Initialize();
//         SubscribeLocalEvent<PointLightColorBlueComponent, ComponentGetState>(OnLightGetState);
//         SubscribeLocalEvent<PointLightColorBlueComponent, ComponentStartup>(OnLightStartup);
//     }

//     private void OnLightStartup(EntityUid uid, PointLightColorBlueComponent component, ComponentStartup args)
//     {
//         UpdatePriority(uid, component, MetaData(uid));
//     }

//     protected override void UpdatePriority(EntityUid uid, SharedPointLightComponent comp, MetaDataComponent meta)
//     {
//         var isHighPriority = comp.Enabled && comp.CastShadows && (comp.Radius > 7);
//         _metadata.SetFlag((uid, meta), MetaDataFlags.PvsPriority, isHighPriority);
//     }

//     private void OnLightGetState(EntityUid uid, PointLightColorBlueComponent component, ref ComponentGetState args)
//     {
//         args.State = new PointLightComponentState()
//         {
//             Color = component.Color,
//             Enabled = component.Enabled,
//             Energy = component.Energy,
//             Offset = component.Offset,
//             Radius = component.Radius,
//             Softness = component.Softness,
//             CastShadows = component.CastShadows,
//         };
//     }

//     public override SharedPointLightComponent EnsureLight(EntityUid uid)
//     {
//         return EnsureComp<PointLightColorBlueComponent>(uid);
//     }

//     public override bool ResolveLight(EntityUid uid, [NotNullWhen(true)] ref SharedPointLightComponent? component)
//     {
//         if (component is not null)
//             return true;

//         TryComp<PointLightColorBlueComponent>(uid, out var comp);
//         component = comp;
//         return component != null;
//     }

//     public override bool TryGetLight(EntityUid uid, [NotNullWhen(true)] out SharedPointLightComponent? component)
//     {
//         if (TryComp<PointLightColorBlueComponent>(uid, out var comp))
//         {
//             component = comp;
//             return true;
//         }

//         component = null;
//         return false;
//     }

//     public override bool RemoveLightDeferred(EntityUid uid)
//     {
//         return RemCompDeferred<PointLightColorBlueComponent>(uid);
//     }
// }
