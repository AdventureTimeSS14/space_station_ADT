using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Shadowling;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Shadowling;

public sealed partial class ADTShadowlingAbilitySystem
{
    private readonly HashSet<string> _usedNames = new();

    private void InitializeHatch()
    {
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingHatchEvent>(OnHatch);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingHatchDoAfterEvent>(OnHatchDoAfter);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
     }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _usedNames.Clear();
    }

    private void OnHatch(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingHatchEvent args)
    {
        if (args.Handled || ent.Comp.Hatched)
            return;

        if (_gameTicker.RoundDuration() < ent.Comp.RoundDurationCooldown)
        {
            var remaining = ent.Comp.RoundDurationCooldown - _gameTicker.RoundDuration();
            var minutes = (int)Math.Ceiling(remaining.TotalMinutes);

            _popup.PopupEntity(
                Loc.GetString("shadowling-round-duration-cooldown", ("minutes", minutes)),
                ent,
                ent);

            return;
        }

        if (ent.Comp.HatchStages.Count == 0)
            return;

        if (Transform(ent).GridUid == null)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-hatch-need-floor"), ent, ent);
            return;
        }

        _popup.PopupEntity(Loc.GetString("shadowling-hatch-begin-self"), ent, ent, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("shadowling-hatch-begin-others", ("user", ent.Owner)), ent, Filter.PvsExcept(ent.Owner), true, PopupType.MediumCaution);

        DropEverything(ent);
        StartHatchStage(ent, 0);

        args.Handled = true;
    }

    private void StartHatchStage(Entity<ADTShadowlingComponent> ent, int stage)
    {
        var args = new DoAfterArgs(EntityManager, ent, ent.Comp.HatchStages[stage], new ADTShadowlingHatchDoAfterEvent(stage), ent)
        {
            BreakOnDamage = false,
            BreakOnHandChange = false,
            BreakOnMove = false,
            CancelDuplicate = false,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnHatchDoAfter(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingHatchDoAfterEvent args)
    {
        if (args.Handled || ent.Comp.Hatched)
            return;

        if (args.Cancelled)
        {
            BreakChrysalis(ent);
            return;
        }

        args.Handled = true;

        if (args.Stage == 0)
            GrowChrysalis(ent);

        _popup.PopupEntity(Loc.GetString("shadowling-hatch-stage", ("stage", args.Stage)), ent, ent, PopupType.MediumCaution);

        if (args.Stage + 1 < ent.Comp.HatchStages.Count)
        {
            StartHatchStage(ent, args.Stage + 1);
            return;
        }

        FinishHatch(ent);
    }

    private void FinishHatch(Entity<ADTShadowlingComponent> ent)
    {
        BreakChrysalis(ent);
        _audio.PlayPvs(ent.Comp.HatchSound, ent);

        var newForm = _polymorph.PolymorphEntity(ent, ent.Comp.HatchPolymorph);
        if (newForm == null)
            return;

        if (TryComp<ADTShadowlingComponent>(newForm.Value, out var hatched))
        {
            hatched.UnlockedAbilities = new HashSet<EntProtoId>(ent.Comp.UnlockedAbilities);
            hatched.AscensionUnlocked = ent.Comp.AscensionUnlocked;
            hatched.KnownThralls = ent.Comp.KnownThralls;
            _shadowling.RestoreProgress((newForm.Value, hatched));

            if (PickName(hatched) is { } name)
                _metaData.SetEntityName(newForm.Value, name);
        }

        _popup.PopupEntity(Loc.GetString("shadowling-hatch-alive"), newForm.Value, newForm.Value, PopupType.LargeCaution);
    }

    private void GrowChrysalis(Entity<ADTShadowlingComponent> ent)
    {
        var coords = Transform(ent).Coordinates;

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                var wall = Spawn(ent.Comp.ChrysalisProto, coords.Offset(new Vector2(x, y)));
                var comp = EnsureComp<ADTShadowlingChrysalisComponent>(wall);
                comp.Shadowling = ent;
            }
        }
    }

    private void BreakChrysalis(Entity<ADTShadowlingComponent> ent)
    {
        var any = false;
        var query = EntityQueryEnumerator<ADTShadowlingChrysalisComponent>();
        while (query.MoveNext(out var uid, out var chrysalis))
        {
            if (chrysalis.Shadowling != ent.Owner)
                continue;

            QueueDel(uid);
            any = true;
        }

        if (any)
            _audio.PlayPvs(ent.Comp.ChrysalisBreakSound, ent);
    }

    private void DropEverything(EntityUid uid)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return;

        foreach (var held in _hands.EnumerateHeld((uid, hands)).ToList())
        {
            _hands.TryDrop((uid, hands), held, checkActionBlocker: false);
        }
    }

    private string? PickName(ADTShadowlingComponent comp)
    {
        if (!_proto.TryIndex(comp.NameDataset, out var dataset))
            return null;

        var available = dataset.Values.Where(name => !_usedNames.Contains(name)).ToList();
        if (available.Count == 0)
            return null;

        var picked = _random.Pick(available);
        _usedNames.Add(picked);
        return picked;
    }
}
