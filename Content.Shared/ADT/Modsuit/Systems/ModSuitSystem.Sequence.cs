using Content.Shared.Clothing.Components;
using Robust.Shared.Network;

namespace Content.Shared.ADT.ModSuits;

public sealed partial class ModSuitSystem
{
    /// <summary>
    /// Порядок развёртывания: снизу вверх. Складывается костюм в обратном порядке.
    /// </summary>
    private static readonly string[] DeployOrder = { "shoes", "outerClothing", "gloves", "head" };

    private void InitializeSequence()
    {
        SubscribeLocalEvent<ModSuitComponent, ToggleAllModSuitPartsMessage>(OnToggleAllMessage);
    }

    private void OnToggleAllMessage(Entity<ModSuitComponent> ent, ref ToggleAllModSuitPartsMessage args)
    {
        if (!_netMan.IsServer)
            return;

        var deploying = GetPartsToggleStatus(ent.Owner, ent.Comp) != ModSuitAttachedStatus.AllToggled;

        var slots = new List<string>(DeployOrder);
        if (!deploying)
            slots.Reverse();

        var queue = new List<EntityUid>();

        foreach (var slot in slots)
        {
            foreach (var (netPart, partSlot) in ent.Comp.ClothingUids)
            {
                if (partSlot != slot)
                    continue;

                var part = GetEntity(netPart);

                if (!Exists(part))
                    continue;

                if (ent.Comp.PartsContainer.Contains(part) == deploying)
                    queue.Add(part);
            }
        }

        if (queue.Count == 0)
            return;

        ent.Comp.PendingSequence = queue;
        ent.Comp.SequenceUser = args.Actor;
        ent.Comp.NextSequenceTime = _timing.CurTime;
    }

    private void UpdateSequences()
    {
        if (!_netMan.IsServer)
            return;

        var query = EntityQueryEnumerator<ModSuitComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PendingSequence.Count == 0)
                continue;

            if (_timing.CurTime < comp.NextSequenceTime)
                continue;

            var part = comp.PendingSequence[0];
            comp.PendingSequence.RemoveAt(0);
            comp.NextSequenceTime = _timing.CurTime + comp.SequenceDelay;

            if (comp.SequenceUser is not { } user || !Exists(user) || !Exists(part))
            {
                ClearSequence(comp);
                continue;
            }

            var wasDeployed = !comp.PartsContainer.Contains(part);

            TogglePart((uid, comp), user, part);

            // Звук шага предсказывал бы клиент, но очередь крутится только на сервере:
            // PlayPredicted внутри инвентаря как раз исключает носителя, доигрываем ему сами.
            if (TryComp<ClothingComponent>(part, out var clothing))
            {
                var stepSound = wasDeployed ? clothing.UnequipSound : clothing.EquipSound;
                _audio.PlayEntity(stepSound, user, part);
            }

            if (GetPartsToggleStatus(uid, comp) == ModSuitAttachedStatus.AllToggled)
                _audio.PlayGlobal(comp.FullyEnabledSound, user);
        }
    }

    private static void ClearSequence(ModSuitComponent comp)
    {
        comp.PendingSequence.Clear();
        comp.SequenceUser = null;
    }
}
