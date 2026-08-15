using System.Linq;
using Content.Server.Construction.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.ADT.Construction.Prototypes;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private void InitializeMachineUpgrades()
    {
        SubscribeLocalEvent<MachineComponent, GetVerbsEvent<ExamineVerb>>(OnMachineExaminableVerb);
        SubscribeLocalEvent<MachineComponent, MachineDeconstructedEvent>(OnMachineDeconstructed);
    }

    private void OnMachineExaminableVerb(EntityUid uid, MachineComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var markup = new FormattedMessage();
        RaiseLocalEvent(uid, new UpgradeExamineEvent(ref markup));

        if (markup.IsEmpty)
            return;

        if (!FormattedMessage.TryFromMarkup(markup.ToMarkup().TrimEnd('\n'), out markup))
            markup = FormattedMessage.Empty;

        if (markup.IsEmpty)
            return;

        args.Verbs.Add(new ExamineVerb
        {
            Act = () => _examineSystem.SendExamineTooltip(args.User, uid, markup, getVerbs: false, centerAtCursor: false),
            Text = Loc.GetString("machine-upgrade-examinable-verb-text"),
            Message = Loc.GetString("machine-upgrade-examinable-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png"))
        });
    }

    public bool GetMachinePartState(EntityUid uid, out MachinePartState state)
    {
        state = new MachinePartState();

        if (!TryComp(uid, out MachinePartComponent? part))
            return false;

        state.Part = part;
        TryComp(uid, out state.Stack);
        return true;
    }

    private void AbsorbContainerParts(EntityUid uid, MachineComponent component, MachinePartStorageComponent storage)
    {
        if (component.PartContainer.ContainedEntities.Count == 0)
            return;

        Dictionary<ProtoId<MachinePartPrototype>, int>? requirements = null;
        if (component.BoardContainer.ContainedEntities.Count > 0
            && TryComp<MachineBoardComponent>(component.BoardContainer.ContainedEntities[0], out var board))
        {
            requirements = board.PartRequirements;
        }

        foreach (var partUid in component.PartContainer.ContainedEntities.ToArray())
        {
            if (!TryComp(partUid, out MachinePartComponent? part))
                continue;

            if (requirements != null && !requirements.ContainsKey(part.Part))
                continue;

            if (EntityManager.IsQueuedForDeletion(partUid))
                continue;

            var quantity = TryComp(partUid, out StackComponent? stack) ? stack.Count : 1;
            storage.Parts.Add(new MachinePartSlot { Part = part.Part, Tier = part.Tier, Quantity = quantity });
            QueueDel(partUid);
        }
    }

    private void OnMachineDeconstructed(EntityUid uid, MachineComponent component, MachineDeconstructedEvent args)
    {
        if (!TryComp<MachinePartStorageComponent>(uid, out var storage) || storage.Parts.Count == 0)
            return;

        foreach (var slot in storage.Parts)
        {
            if (!_proto.TryIndex(slot.Part, out var partProto))
                continue;

            for (var i = 0; i < slot.Quantity; i++)
            {
                if (!TrySpawnInContainer(partProto.GetTierPrototype(slot.Tier), uid, MachineFrameComponent.PartContainerName, out _))
                    Log.Error($"Couldn't materialize machine part {slot.Part} from machine {Prototype(uid)?.ID ?? "N/A"}!");
            }
        }

        storage.Parts.Clear();
    }

    private Dictionary<ProtoId<MachinePartPrototype>, float> GetPartRatings(List<MachinePartSlot> slots)
    {
        var weightedRatings = new Dictionary<ProtoId<MachinePartPrototype>, float>();
        var quantities = new Dictionary<ProtoId<MachinePartPrototype>, float>();

        foreach (var slot in slots)
        {
            var id = slot.Part;
            var count = slot.Quantity;

            quantities[id] = quantities.GetValueOrDefault(id) + count;
            weightedRatings[id] = weightedRatings.GetValueOrDefault(id) + slot.Tier * count;
        }

        var result = new Dictionary<ProtoId<MachinePartPrototype>, float>();
        foreach (var (id, quantity) in quantities)
        {
            result[id] = quantity > 0 ? weightedRatings[id] / quantity : 1f;
        }

        return result;
    }

    public void RefreshParts(EntityUid uid, MachineComponent component)
    {
        var storage = EnsureComp<MachinePartStorageComponent>(uid);
        AbsorbContainerParts(uid, component, storage);

        var partRatings = GetPartRatings(storage.Parts);
        var statMultipliers = GetStatMultipliers(partRatings);

        RaiseLocalEvent(uid,
            new RefreshPartsEvent
            {
                PartRatings = partRatings,
                StatMultipliers = statMultipliers,
            },
            broadcast: true);
    }

    private Dictionary<MachineStat, float> GetStatMultipliers(
        Dictionary<ProtoId<MachinePartPrototype>, float> partRatings)
    {
        var multipliers = new Dictionary<MachineStat, float>();

        foreach (var (partId, rating) in partRatings)
        {
            if (!_proto.TryIndex(partId, out var partProto))
                continue;

            foreach (var (stat, value) in partProto.Stats)
            {
                var partMultiplier = RefreshPartsEvent.GetTierMultiplier(rating, value);
                multipliers[stat] = multipliers.GetValueOrDefault(stat, 1f) * partMultiplier;
            }
        }

        return multipliers;
    }
}
