using System.Text;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.ADT.GPS;

public abstract class SharedGpsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GpsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GpsComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<GpsComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GpsComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<GpsComponent, EmpDisabledRemovedEvent>(OnEmpDisabledRemoved);

        SubscribeLocalEvent<GpsSignalComponent, MobStateChangedEvent>(OnSignalMobStateChanged);
    }

    private void OnMapInit(Entity<GpsComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnGetAltVerbs(Entity<GpsComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !ent.Comp.CanToggle)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = ent.Comp.Tracking
                ? Loc.GetString("adt-gps-verb-disable")
                : Loc.GetString("adt-gps-verb-enable"),
            Act = () => TryToggle(ent, user),
        });
    }

    private void OnExamined(Entity<GpsComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("adt-gps-examine-tag", ("tag", ent.Comp.Tag)));

        var stateMessage = ent.Comp.Tracking
            ? "adt-gps-examine-tracking"
            : "adt-gps-examine-idle";

        args.PushMarkup(Loc.GetString(stateMessage));
    }

    private void OnEmpPulse(Entity<GpsComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        UpdateAppearance(ent, emped: true);
    }

    private void OnEmpDisabledRemoved(Entity<GpsComponent> ent, ref EmpDisabledRemovedEvent args)
    {
        UpdateAppearance(ent, emped: false);
    }

    private void OnSignalMobStateChanged(Entity<GpsSignalComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.DisableOnDeath)
            return;

        ent.Comp.Enabled = args.NewMobState != MobState.Dead;
    }

    public void TryToggle(Entity<GpsComponent> ent, EntityUid user)
    {
        if (!ent.Comp.CanToggle)
            return;

        if (HasComp<EmpDisabledComponent>(ent))
        {
            _popup.PopupEntity(Loc.GetString("adt-gps-popup-broken"), ent.Owner, user);
            return;
        }

        SetTracking(ent, !ent.Comp.Tracking);

        var message = ent.Comp.Tracking
            ? "adt-gps-popup-enabled"
            : "adt-gps-popup-disabled";

        _popup.PopupEntity(Loc.GetString(message), ent.Owner, user);
    }

    public void SetTracking(Entity<GpsComponent> ent, bool tracking)
    {
        if (ent.Comp.Tracking == tracking)
            return;

        ent.Comp.Tracking = tracking;
        Dirty(ent);

        UpdateAppearance(ent);
        UpdateUiState(ent);
    }

    public void SetSameMapOnly(Entity<GpsComponent> ent, bool sameMapOnly)
    {
        if (ent.Comp.SameMapOnly == sameMapOnly)
            return;

        ent.Comp.SameMapOnly = sameMapOnly;
        Dirty(ent);

        UpdateUiState(ent);
    }

    public void SetTag(Entity<GpsComponent> ent, string tag)
    {
        var builder = new StringBuilder(ent.Comp.MaxTagLength);

        foreach (var character in tag.Trim())
        {
            if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                continue;

            builder.Append(char.ToUpperInvariant(character));

            if (builder.Length == ent.Comp.MaxTagLength)
                break;
        }

        var sanitized = builder.ToString();

        if (sanitized.Length == 0 || sanitized == ent.Comp.Tag)
            return;

        ent.Comp.Tag = sanitized;
        Dirty(ent);

        UpdateUiState(ent);
    }

    private void UpdateAppearance(Entity<GpsComponent> ent)
    {
        UpdateAppearance(ent, HasComp<EmpDisabledComponent>(ent));
    }

    private void UpdateAppearance(Entity<GpsComponent> ent, bool emped)
    {
        _appearance.SetData(ent, GpsVisuals.Emped, emped);
        _appearance.SetData(ent, GpsVisuals.Tracking, ent.Comp.Tracking && !emped);
    }

    protected virtual void UpdateUiState(Entity<GpsComponent> ent)
    {
    }
}
