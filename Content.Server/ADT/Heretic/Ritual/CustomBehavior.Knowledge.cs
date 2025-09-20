using Content.Server.Heretic.EntitySystems;
using Content.Shared.Dataset;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;
using Robust.Server.Containers;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualKnowledgeBehavior : RitualCustomBehavior
{
    // я полностью переписала систему, при апстриме оставляем нашу версию
    private static Dictionary<ProtoId<TagPrototype>, int> requiredTags = new();
    private static List<string> requiredTagsLocale = new();
    private List<EntityUid> toDelete = new();

    private IPrototypeManager _prot = default!;
    private IRobustRandom _rand = default!;
    private EntityLookupSystem _lookup = default!;
    private HereticSystem _heretic = default!;
    private ContainerSystem _container = default!;

    [ValidatePrototypeId<DatasetPrototype>]
    public const string EligibleTagsDataset = "EligibleTags";

    public override bool Execute(RitualData args, out string? outstr)
    {
        _prot = IoCManager.Resolve<IPrototypeManager>();
        _rand = IoCManager.Resolve<IRobustRandom>();
        _lookup = args.EntityManager.System<EntityLookupSystem>();
        _heretic = args.EntityManager.System<HereticSystem>();
        _container = args.EntityManager.System<ContainerSystem>();

        outstr = null;

        if (requiredTags.Count == 0)
        {
            var allTags = _prot.Index<DatasetPrototype>(EligibleTagsDataset).Values.ToList();

            if (allTags.Count < 5)
            {
                outstr = Loc.GetString("heretic-ritual-fail-not-enough-tags");
                return false;
            }

            var selectedTags = new HashSet<string>();
            while (selectedTags.Count < 5 && selectedTags.Count < allTags.Count)
            {
                var tagIndex = _rand.Next(allTags.Count);
                selectedTags.Add(allTags[tagIndex]);
            }

            foreach (var tag in selectedTags)
            {
                var protoId = new ProtoId<TagPrototype>(tag);
                requiredTags.Add(protoId, 1);
                requiredTagsLocale.Add(Loc.GetString("names-eligibleTags-" + tag));
            }
        }

        var lookup = _lookup.GetEntitiesInRange(args.Platform, .75f);

        var workingRequiredTags = new Dictionary<ProtoId<TagPrototype>, int>(requiredTags);

        foreach (var entity in lookup)
        {
            if (_container.IsEntityInContainer(entity))
                continue;

            if (!args.EntityManager.TryGetComponent<TagComponent>(entity, out var tagComponent))
                continue;

            var entityTags = tagComponent.Tags;

            foreach (var requiredTag in workingRequiredTags.Keys.ToList())
            {
                if (workingRequiredTags[requiredTag] <= 0)
                    continue;

                if (entityTags.Contains(requiredTag))
                {
                    workingRequiredTags[requiredTag]--;
                    toDelete.Add(entity);
                    break;
                }
            }
        }

        var missingList = new List<ProtoId<TagPrototype>>();
        foreach (var tag in workingRequiredTags)
        {
            if (tag.Value > 0)
                missingList.Add(tag.Key);
        }

        if (missingList.Count > 0)
        {
            var missingListLocaled = new List<string>();
            foreach (var missingTag in missingList)
            {
                missingListLocaled.Add(Loc.GetString("names_eligibleTags-" + missingTag.Id));
            }

            var sb = new StringBuilder();
            for (int i = 0; i < missingListLocaled.Count; i++)
            {
                if (i == missingListLocaled.Count - 1)
                    sb.Append(missingListLocaled[i]);
                else
                    sb.Append($"{missingListLocaled[i]}, ");
            }

            outstr = Loc.GetString("heretic-ritual-fail-items", ("itemlist", sb.ToString()));
            return false;
        }

        return true;
    }

    public override void Finalize(RitualData args)
    {
        foreach (var ent in toDelete)
        {
            args.EntityManager.QueueDeleteEntity(ent);
        }
        toDelete.Clear();

        if (args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
        {
            _heretic.UpdateKnowledge(args.Performer, hereticComp, 4);
        }

        requiredTags.Clear();
        requiredTagsLocale.Clear();
    }
}