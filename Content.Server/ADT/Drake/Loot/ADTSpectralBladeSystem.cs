using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.ADT.Drake.Loot;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake.Loot;

public sealed class ADTSpectralBladeSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    private static readonly ProtoId<TagPrototype> AllowGhostShownByEventTag = "AllowGhostShownByEvent";

    private readonly HashSet<EntityUid> _spiritBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTSpectralBladeComponent, ComponentShutdown>(OnBladeShutdown);
        SubscribeLocalEvent<ADTSpectralBladeComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<ADTSpectralBladeComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<ADTSpectralBladeComponent, EntityStartedFollowingEvent>(OnBladeFollowed);
        SubscribeLocalEvent<ADTSpectralBladeComponent, EntityStoppedFollowingEvent>(OnBladeUnfollowed);
        SubscribeLocalEvent<ADTSpectralBladeComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ADTSpectralBladeComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<ADTSpectralBladeComponent, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<ADTSpectralBladeWielderComponent, EntityStartedFollowingEvent>(OnWielderFollowed);
        SubscribeLocalEvent<ADTSpectralBladeWielderComponent, EntityStoppedFollowingEvent>(OnWielderUnfollowed);
        SubscribeLocalEvent<ADTSpectralBladeWielderComponent, BeforeDamageChangedEvent>(OnWielderBeforeDamage);
    }

    private void OnBladeShutdown(Entity<ADTSpectralBladeComponent> ent, ref ComponentShutdown args)
    {
        foreach (var spirit in ent.Comp.Spirits)
        {
            HideSpirit(spirit);
        }

        ent.Comp.Spirits.Clear();
        SetWielder(ent, null);
    }

    private void OnEquippedHand(Entity<ADTSpectralBladeComponent> ent, ref GotEquippedHandEvent args)
    {
        SetWielder(ent, args.User);
        UpdateSpirits(ent);
    }

    private void OnUnequippedHand(Entity<ADTSpectralBladeComponent> ent, ref GotUnequippedHandEvent args)
    {
        SetWielder(ent, null);
        UpdateSpirits(ent);
    }

    private void OnBladeFollowed(Entity<ADTSpectralBladeComponent> ent, ref EntityStartedFollowingEvent args)
    {
        UpdateSpirits(ent);
    }

    private void OnBladeUnfollowed(Entity<ADTSpectralBladeComponent> ent, ref EntityStoppedFollowingEvent args)
    {
        UpdateSpirits(ent);
    }

    private void OnWielderFollowed(Entity<ADTSpectralBladeWielderComponent> ent, ref EntityStartedFollowingEvent args)
    {
        UpdateWielderBlades(ent);
    }

    private void OnWielderUnfollowed(Entity<ADTSpectralBladeWielderComponent> ent, ref EntityStoppedFollowingEvent args)
    {
        UpdateWielderBlades(ent);
    }

    private void OnUseInHand(Entity<ADTSpectralBladeComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var now = _timing.CurTime;
        if (ent.Comp.NextSummon > now)
        {
            _popup.PopupEntity(Loc.GetString("adt-spectral-blade-summon-cooldown"), args.User, args.User);
            return;
        }

        ent.Comp.NextSummon = now + ent.Comp.SummonCooldown;
        _popup.PopupEntity(Loc.GetString("adt-spectral-blade-summon"), args.User, args.User);

        var message = Loc.GetString("adt-spectral-blade-summon-ghosts", ("user", args.User), ("blade", ent.Owner));
        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        var ghosts = Filter.Empty().AddWhereAttachedEntity(HasComp<GhostComponent>);
        _chat.ChatMessageToManyFiltered(ghosts, ChatChannel.Server, message, wrapped, ent, false, true, null);
    }

    private void OnGetMeleeDamage(Entity<ADTSpectralBladeComponent> ent, ref GetMeleeDamageEvent args)
    {
        var damage = ent.Comp.DamagePerSpirit * (float) ent.Comp.Spirits.Count;
        var total = damage.GetTotal();

        if (total > ent.Comp.MaxDamage)
            damage *= ent.Comp.MaxDamage / total;

        args.Damage = damage;
    }

    private void OnMeleeHit(Entity<ADTSpectralBladeComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        var count = ent.Comp.Spirits.Count;
        _popup.PopupEntity(Loc.GetString("adt-spectral-blade-hit-self", ("count", count)), args.User, args.User, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("adt-spectral-blade-hit-others", ("user", args.User), ("count", count)), args.User, Filter.PvsExcept(args.User), true, PopupType.MediumCaution);
    }

    private void OnWielderBeforeDamage(Entity<ADTSpectralBladeWielderComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || args.Origin is not { } origin || origin == ent.Owner)
            return;

        if (!args.Damage.AnyPositive())
            return;

        var chance = 0f;
        var count = 0;

        foreach (var blade in ent.Comp.Blades)
        {
            if (!TryComp<ADTSpectralBladeComponent>(blade, out var comp))
                continue;

            var bladeChance = MathF.Min(comp.Spirits.Count * comp.BlockChancePerSpirit, comp.MaxBlockChance);
            if (bladeChance <= chance)
                continue;

            chance = bladeChance;
            count = comp.Spirits.Count;
        }

        if (chance <= 0f || !_random.Prob(chance))
            return;

        args.Cancelled = true;
        _popup.PopupEntity(Loc.GetString("adt-spectral-blade-block", ("user", ent.Owner), ("count", count)), ent, PopupType.MediumCaution);
    }

    private void SetWielder(Entity<ADTSpectralBladeComponent> ent, EntityUid? wielder)
    {
        if (ent.Comp.Wielder == wielder)
            return;

        if (ent.Comp.Wielder is { } old && TryComp<ADTSpectralBladeWielderComponent>(old, out var oldComp))
        {
            oldComp.Blades.Remove(ent);

            if (oldComp.Blades.Count == 0)
                RemComp(old, oldComp);
        }

        ent.Comp.Wielder = wielder;

        if (wielder is { } user)
            EnsureComp<ADTSpectralBladeWielderComponent>(user).Blades.Add(ent);
    }

    private void UpdateWielderBlades(Entity<ADTSpectralBladeWielderComponent> ent)
    {
        foreach (var blade in ent.Comp.Blades)
        {
            if (TryComp<ADTSpectralBladeComponent>(blade, out var comp))
                UpdateSpirits((blade, comp));
        }
    }

    private void UpdateSpirits(Entity<ADTSpectralBladeComponent> ent)
    {
        _spiritBuffer.Clear();
        CollectSpirits(ent, _spiritBuffer);

        if (ent.Comp.Wielder is { } wielder)
            CollectSpirits(wielder, _spiritBuffer);

        foreach (var spirit in ent.Comp.Spirits)
        {
            if (!_spiritBuffer.Contains(spirit))
                HideSpirit(spirit);
        }

        foreach (var spirit in _spiritBuffer)
        {
            if (!ent.Comp.Spirits.Contains(spirit))
                ShowSpirit(spirit);
        }

        ent.Comp.Spirits.Clear();
        ent.Comp.Spirits.UnionWith(_spiritBuffer);
    }

    private void CollectSpirits(EntityUid uid, HashSet<EntityUid> spirits)
    {
        if (!TryComp<FollowedComponent>(uid, out var followed))
            return;

        foreach (var follower in followed.Following)
        {
            if (TerminatingOrDeleted(follower) || !HasComp<GhostComponent>(follower))
                continue;

            if (!_tag.HasTag(follower, AllowGhostShownByEventTag))
                continue;

            spirits.Add(follower);
        }
    }

    private void ShowSpirit(EntityUid spirit)
    {
        var comp = EnsureComp<ADTSpectralSpiritComponent>(spirit);
        comp.Blades++;

        if (comp.Blades > 1 || !TryComp<VisibilityComponent>(spirit, out var visibility))
            return;

        if (_gameTicker.RunLevel == GameRunLevel.PostRound)
            return;

        _visibility.AddLayer((spirit, visibility), (int) VisibilityFlags.Normal, false);
        _visibility.RemoveLayer((spirit, visibility), (int) VisibilityFlags.Ghost, false);
        _visibility.RefreshVisibility(spirit, visibilityComponent: visibility);
    }

    private void HideSpirit(EntityUid spirit)
    {
        if (TerminatingOrDeleted(spirit) || !TryComp<ADTSpectralSpiritComponent>(spirit, out var comp))
            return;

        comp.Blades--;

        if (comp.Blades > 0)
            return;

        RemComp(spirit, comp);

        if (_gameTicker.RunLevel == GameRunLevel.PostRound || !TryComp<VisibilityComponent>(spirit, out var visibility))
            return;

        _visibility.AddLayer((spirit, visibility), (int) VisibilityFlags.Ghost, false);
        _visibility.RemoveLayer((spirit, visibility), (int) VisibilityFlags.Normal, false);
        _visibility.RefreshVisibility(spirit, visibilityComponent: visibility);
    }
}
