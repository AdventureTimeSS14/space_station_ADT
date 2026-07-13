using Content.Server.Heretic.Components;
using Content.Server.Heretic.Ritual;
using Content.Server.Chat.Managers;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using System.Text;
using System.Linq;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Examine;
using Content.Shared.ADT.Heretic.Components;
using Robust.Shared.Containers;
using Content.Shared.Chat;
using Robust.Server.Player;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticRitualSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _series = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HereticKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public SoundSpecifier RitualSuccessSound = new SoundPathSpecifier("/Audio/ADT/Heretic/castsummon.ogg");

    /// <summary>
    ///     Отправляет рецепт ритуала в чат игроку
    /// </summary>
    private void SendRitualRecipeToChat(EntityUid performer, HereticRitualPrototype ritual)
    {
        var sb = new StringBuilder();

        // Заголовок ритуала
        var ritualName = Loc.GetString(ritual.LocName);
        sb.AppendLine($"[color=#FF6B35]═══ {ritualName} ═══[/color]");

        // Требуемые предметы (теги)
        if (ritual.RequiredTags != null && ritual.RequiredTags.Count > 0)
        {
            sb.AppendLine($"[color=#AAAAAA]{Loc.GetString("heretic-ritual-recipe-required-items")}[/color]");
            foreach (var tag in ritual.RequiredTags)
            {
                var tagKey = $"names_eligibleTags-{tag.Key.Id}";
                var tagName = Loc.HasString(tagKey) ? Loc.GetString(tagKey) : tag.Key.Id;
                var countStr = tag.Value > 1 ? $" x{tag.Value}" : "";
                sb.AppendLine($"  • {tagName}{countStr}");
            }
        }

        // Требуемые имена сущностей
        if (ritual.RequiredEntityNames != null && ritual.RequiredEntityNames.Count > 0)
        {
            sb.AppendLine($"[color=#AAAAAA]{Loc.GetString("heretic-ritual-recipe-required-entities")}[/color]");
            foreach (var entityName in ritual.RequiredEntityNames)
            {
                var countStr = entityName.Value > 1 ? $" x{entityName.Value}" : "";
                sb.AppendLine($"  • {entityName.Key}{countStr}");
            }
        }

        if (ritual.CustomBehaviors != null && ritual.CustomBehaviors.Count > 0)
        {
            foreach (var behavior in ritual.CustomBehaviors)
            {
                if (behavior is RitualSacrificeBehavior sacrifice)
                {
                    sb.AppendLine($"[color=#AAAAAA]{Loc.GetString("heretic-ritual-recipe-required-corpses", ("min", sacrifice.Min), ("max", sacrifice.Max))}[/color]");
                }
                if (behavior is RitualAscensionSacrificeBehavior ascensionSacrifice)
                {
                    sb.AppendLine($"[color=#AAAAAA]{Loc.GetString("heretic-ritual-recipe-required-corpses-ascension", ("min", ascensionSacrifice.Min), ("max", ascensionSacrifice.Max))}[/color]");
                }
                if (behavior is RitualTemperatureBehavior temp)
                {
                    if (temp.MinThreshold <= 0)
                        sb.AppendLine($"[color=#AAAAAA]{Loc.GetString("heretic-ritual-recipe-requirement-cold", ("temp", temp.MinThreshold))}[/color]");
                    else
                        sb.AppendLine($"[color=#AAAAAA]{Loc.GetString("heretic-ritual-recipe-requirement-hot", ("temp", temp.MinThreshold))}[/color]");
                }
                if (behavior is RitualReagentPuddleBehavior reagent && reagent.Reagent != null)
                {
                    sb.AppendLine($"[color=#AAAAAA]{Loc.GetString("heretic-ritual-recipe-required-reagent", ("reagent", reagent.Reagent.Value))}[/color]");
                }
                // Обработка ритуала знаний - показывает текущие требуемые предметы
                if (behavior is RitualKnowledgeBehavior)
                {
                    var knowledgeTags = RitualKnowledgeBehavior.GetRequiredTags();
                    if (knowledgeTags.Count > 0)
                    {
                        sb.AppendLine($"[color=#AAAAAA]{Loc.GetString("heretic-ritual-recipe-required-items")}[/color]");
                        foreach (var tag in knowledgeTags)
                        {
                            var tagKey = $"names_eligibleTags-{tag.Key.Id}";
                            var tagName = Loc.HasString(tagKey) ? Loc.GetString(tagKey) : tag.Key.Id;
                            var countStr = tag.Value > 1 ? $" x{tag.Value}" : "";
                            sb.AppendLine($"  • {tagName}{countStr}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"[color=#AAAAAA]{Loc.GetString("heretic-ritual-recipe-required-items-knowledge")}[/color]");
                    }
                }
            }
        }

        var message = sb.ToString();
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

        if (_playerManager.TryGetSessionByEntity(performer, out var session))
        {
            _chatManager.ChatMessageToOne(ChatChannel.Server,
                message,
                wrappedMessage,
                default,
                false,
                session.Channel,
                Color.FromHex("#FF9632"));
        }
    }

    public HereticRitualPrototype GetRitual(ProtoId<HereticRitualPrototype>? id)
    {
        if (id == null) throw new ArgumentNullException();
        return _proto.Index<HereticRitualPrototype>(id);
    }

    /// <summary>
    ///     Try to perform a selected ritual
    /// </summary>
    /// <returns> If the ritual succeeded or not </returns>
    public bool TryDoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // here i'm introducing locals for basically everything
        // because if i access stuff directly shit is bound to break.
        // please don't access stuff directly from the prototypes or else shit will break.
        // regards

        if (!TryComp<HereticComponent>(performer, out var hereticComp))
            return false;

        var rit = _series.CreateCopy((HereticRitualPrototype) GetRitual(ritualId).Clone(), notNullableOverride: true);
        var lookup = _lookup.GetEntitiesInRange(platform, 1.5f);

        var missingList = new Dictionary<string, float>();
        var toDelete = new List<EntityUid>();

        // check for all conditions
        // this is god awful but it is that it is
        var behaviors = rit.CustomBehaviors ?? new();
        var requiredTags = rit.RequiredTags?.ToDictionary(e => e.Key, e => e.Value) ?? new();

        foreach (var behavior in behaviors)
        {
            var ritData = new RitualData(performer, platform, ritualId, EntityManager);

            if (!behavior.Execute(ritData, out var missingStr))
            {
                if (missingStr != null)
                    _popup.PopupEntity(missingStr, platform, performer);
                return false;
            }
        }

        foreach (var look in lookup)
        {
            // check for matching tags
            foreach (var tag in requiredTags)
            {
                if (!TryComp<TagComponent>(look, out var tags) // no tags?
                || _container.IsEntityInContainer(look)) // using your own eyes for amber focus?
                    continue;

                var ltags = tags.Tags;

                if (ltags.Contains(tag.Key))
                {
                    requiredTags[tag.Key] -= 1;

                    // prevent deletion of more items than needed
                    if (requiredTags[tag.Key] >= 0)
                        toDelete.Add(look);
                }
            }
        }

        // add missing tags
        foreach (var tag in requiredTags)
            if (tag.Value > 0)
                missingList.Add(tag.Key, tag.Value);

        // are we missing anything?
        if (missingList.Count > 0)
        {
            // we are! notify the performer about that!
            var sb = new StringBuilder();
            for (int i = 0; i < missingList.Keys.Count; i++)
            {
                var key = missingList.Keys.ToList()[i];
                var missing = $"{key} x{missingList[key]}";

                // makes a nice, list, of, missing, items.
                if (i != missingList.Count - 1)
                    sb.Append($"{missing}, ");
                else sb.Append(missing);
            }

            _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-items", ("itemlist", sb.ToString())), platform, performer);
            return false;
        }

        // yay! ritual successfull!

        // reset fields to their initial values
        // BECAUSE FOR SOME REASON IT DOESN'T FUCKING WORK OTHERWISE!!!

        // finalize all of the custom ones
        foreach (var behavior in behaviors)
        {
            var ritData = new RitualData(performer, platform, ritualId, EntityManager);
            behavior.Finalize(ritData);
        }

        // ya get some, ya lose some
        foreach (var ent in toDelete)
            QueueDel(ent);

        // add stuff
        var output = rit.Output ?? new();
        foreach (var ent in output.Keys)
            for (int i = 0; i < output[ent]; i++)
                Spawn(ent, Transform(platform).Coordinates);

        if (rit.OutputEvent != null)
            RaiseLocalEvent(performer, rit.OutputEvent, true);

        if (rit.OutputKnowledge != null)
            _knowledge.AddKnowledge(performer, hereticComp, (ProtoId<HereticKnowledgePrototype>) rit.OutputKnowledge);

        return true;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualRuneComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticRitualRuneComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HereticRitualRuneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HereticRitualRuneComponent, HereticRitualMessage>(OnRitualChosenMessage);
    }

    private void OnInteract(Entity<HereticRitualRuneComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<HereticComponent>(args.User, out var heretic))
            return;

        if (heretic.KnownRituals.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-norituals"), args.User, args.User);
            return;
        }

        _uiSystem.OpenUi(ent.Owner, HereticRitualRuneUiKey.Key, args.User);
    }

    private void OnRitualChosenMessage(Entity<HereticRitualRuneComponent> ent, ref HereticRitualMessage args)
    {
        var user = args.Actor;

        if (!TryComp<HereticComponent>(user, out var heretic))
            return;

        heretic.ChosenRitual = args.ProtoId;

        var ritual = GetRitual(heretic.ChosenRitual);
        var ritualName = Loc.GetString(ritual.LocName);
        _popup.PopupEntity(Loc.GetString("heretic-ritual-switch", ("name", ritualName)), user, user);
        SendRitualRecipeToChat(user, ritual);
    }

    private void OnInteractUsing(Entity<HereticRitualRuneComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<HereticComponent>(args.User, out var heretic))
            return;

        if (!TryComp<MansusGraspComponent>(args.Used, out var grasp))
            return;

        if (heretic.ChosenRitual == null)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-noritual"), args.User, args.User);
            return;
        }

        if (!TryDoRitual(args.User, ent, (ProtoId<HereticRitualPrototype>) heretic.ChosenRitual))
            return;

        _audio.PlayPvs(RitualSuccessSound, ent, AudioParams.Default.WithVolume(-3f));
        _popup.PopupEntity(Loc.GetString("heretic-ritual-success"), ent, args.User);
        Spawn("HereticRuneRitualAnimation", Transform(ent).Coordinates);
    }

    private void OnExamine(Entity<HereticRitualRuneComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<HereticComponent>(args.Examiner, out var h))
            return;

        var ritual = h.ChosenRitual != null ? GetRitual(h.ChosenRitual).LocName : null;
        var name = ritual != null ? Loc.GetString(ritual) : Loc.GetString("heretic-ritual-unknown");
        args.PushMarkup(Loc.GetString("heretic-ritualrune-examine", ("rit", name)));
    }
}
