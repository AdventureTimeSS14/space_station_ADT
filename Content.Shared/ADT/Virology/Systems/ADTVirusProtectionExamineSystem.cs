using System.Linq;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.Armor;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Virology.Systems;

public sealed class ADTVirusProtectionExamineSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVirusProtectionComponent, ArmorExamineEvent>(OnArmorExamine);
        SubscribeLocalEvent<ADTVirusProtectionComponent, GetVerbsEvent<ExamineVerb>>(OnVerbExamine);
    }

    private void OnArmorExamine(Entity<ADTVirusProtectionComponent> ent, ref ArmorExamineEvent args)
    {
        AppendProtection(ent.Comp, args.Msg);
    }

    private void OnVerbExamine(Entity<ADTVirusProtectionComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || HasComp<ArmorComponent>(ent))
            return;

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("adt-virus-protection-examine"));
        AppendProtection(ent.Comp, msg);

        _examine.AddDetailedExamineVerb(args, ent.Comp, msg,
            Loc.GetString("adt-virus-protection-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("adt-virus-protection-examinable-verb-message"));
    }

    private void AppendProtection(ADTVirusProtectionComponent component, FormattedMessage msg)
    {
        var contact = MathF.Round((1f - Math.Clamp(component.Permeability, 0f, 1f)) * 100, 1);

        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("adt-virus-protection-contact-value", ("value", contact)));

        if (component.Airborne is { } airborne)
        {
            var air = MathF.Round(Math.Clamp(airborne, 0f, 1f) * 100, 1);

            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("adt-virus-protection-airborne-value", ("value", air)));
        }

        if (component.Zones.Count == 0)
            return;

        var zones = string.Join(", ", component.Zones.Select(zone => Loc.GetString($"adt-virus-protection-zone-{zone}")));

        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("adt-virus-protection-zones-value", ("zones", zones)));
    }
}
