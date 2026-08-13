using Content.Shared.ADT.Shadowling;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Shadowling;

public sealed class ADTShadowlingStatusIconSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTShadowlingComponent, GetStatusIconsEvent>(OnGetShadowlingIcon);
        SubscribeLocalEvent<ADTShadowlingThrallComponent, GetStatusIconsEvent>(OnGetThrallIcon);
        SubscribeLocalEvent<ADTAscendantShadowlingComponent, GetStatusIconsEvent>(OnGetAscendantIcon);
    }

    private void OnGetShadowlingIcon(Entity<ADTShadowlingComponent> ent, ref GetStatusIconsEvent args)
    {
        AddIcon(ref args, ent.Comp.StatusIcon);
    }

    private void OnGetThrallIcon(Entity<ADTShadowlingThrallComponent> ent, ref GetStatusIconsEvent args)
    {
        AddIcon(ref args, ent.Comp.StatusIcon);
    }

    private void OnGetAscendantIcon(Entity<ADTAscendantShadowlingComponent> ent, ref GetStatusIconsEvent args)
    {
        AddIcon(ref args, ent.Comp.StatusIcon);
    }

    private void AddIcon(ref GetStatusIconsEvent args, ProtoId<FactionIconPrototype> iconId)
    {
        if (_prototype.TryIndex(iconId, out var icon))
            args.StatusIcons.Add(icon);
    }
}
