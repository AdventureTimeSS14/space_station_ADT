using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Random;
using Content.Shared.Preferences.Loadouts.Effects; // Ganimed sponsor
using Robust.Shared.Collections;
using Robust.Shared.Network;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Contains all of the selected data for a role's loadout.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class RoleLoadout : IEquatable<RoleLoadout>
{
    [DataField]
    public ProtoId<RoleLoadoutPrototype> Role;

    [DataField]
    public Dictionary<ProtoId<LoadoutGroupPrototype>, List<Loadout>> SelectedLoadouts = new();

    /// <summary>
    /// Loadout specific name.
    /// </summary>
    public string? EntityName;

    // ADT SAI Custom start
    /// <summary>
    /// Extra data for this loadout.
    /// </summary>
    public Dictionary<string, string> ExtraData = new();
    // ADT SAI Custom end

    /*
     * Loadout-specific data used for validation.
     */

    public int? Points;

    public RoleLoadout(ProtoId<RoleLoadoutPrototype> role)
    {
        Role = role;
    }

    public RoleLoadout Clone()
    {
        var weh = new RoleLoadout(Role);

        foreach (var selected in SelectedLoadouts)
        {
            weh.SelectedLoadouts.Add(selected.Key, new List<Loadout>(selected.Value));
        }
        // ADT SAI Custom start
        foreach (var extra in ExtraData)
        {
            weh.ExtraData.Add(extra.Key, extra.Value);
        }
        // ADT SAI Custom end

        weh.EntityName = EntityName;

        return weh;
    }

    /// <summary>
    /// Ensures all prototypes exist and effects can be applied.
    /// </summary>
    public void EnsureValid(HumanoidCharacterProfile profile, ICommonSession session, IDependencyCollection collection)
    {
        var groupRemove = new ValueList<string>();
        var protoManager = collection.Resolve<IPrototypeManager>();
        var configManager = collection.Resolve<IConfigurationManager>();
        var netManager = collection.Resolve<INetManager>(); // Corvax-Loadouts

        if (!protoManager.TryIndex(Role, out var roleProto))
        {
            EntityName = null;
            SelectedLoadouts.Clear();
            return;
        }

        // Remove name not allowed.
        if (!roleProto.CanCustomizeName)
        {
            EntityName = null;
        }

        // Validate name length
        // TODO: Probably allow regex to be supplied?
        if (EntityName != null)
        {
            var name = EntityName.Trim();
            var maxNameLength = configManager.GetCVar(CCVars.MaxNameLength);

            if (name.Length > maxNameLength)
            {
                EntityName = name[..maxNameLength];
            }

            if (name.Length == 0)
            {
                EntityName = null;
            }
        }

        // In some instances we might not have picked up a new group for existing data.
        foreach (var groupProto in roleProto.Groups)
        {
            // Ganimed sponsor start
            if (!SelectedLoadouts.ContainsKey(groupProto))
                SelectedLoadouts[groupProto] = new List<Loadout>(); 
            // Ganimed sponsor end
        }

        // Reset points to recalculate.
        Points = roleProto.Points;

        foreach (var (group, groupLoadouts) in SelectedLoadouts)
        {
            // Check the group is even valid for this role.
            if (!roleProto.Groups.Contains(group))
            {
                groupRemove.Add(group);
                continue;
            }

            // Dump if Group doesn't exist
            if (!protoManager.TryIndex(group, out var groupProto))
            {
                groupRemove.Add(group);
                continue;
            }

            var loadouts = groupLoadouts[..Math.Min(groupLoadouts.Count, groupProto.MaxLimit)];

            // Validate first
            for (var i = loadouts.Count - 1; i >= 0; i--)
            {
                var loadout = loadouts[i];

                // Old prototype or otherwise invalid.
                if (!protoManager.TryIndex(loadout.Prototype, out var loadoutProto))
                {
                    loadouts.RemoveAt(i);
                    continue;
                }

                // Malicious client maybe, check the group even has it.
                if (!groupProto.Loadouts.Contains(loadout.Prototype))
                {
                    loadouts.RemoveAt(i);
                    continue;
                }

                // Validate the loadout can be applied (e.g. points).
                if (!IsValid(profile, session, loadout.Prototype, collection, out _))
                {
                    loadouts.RemoveAt(i);
                    continue;
                }

                Apply(loadoutProto);
            }

            // Apply defaults if required
            // Technically it's possible for someone to game themselves into loadouts they shouldn't have
            // If you put invalid ones first but that's your fault for not using sensible defaults
            if (loadouts.Count < groupProto.MinLimit)
            {
                foreach (var protoId in groupProto.Loadouts)
                {
                    if (loadouts.Count >= groupProto.MinLimit)
                        break;

                    if (!protoManager.TryIndex(protoId, out var loadoutProto))
                        continue;

                    var defaultLoadout = new Loadout()
                    {
                        Prototype = loadoutProto.ID,
                    };

                    if (loadouts.Contains(defaultLoadout))
                        continue;

                    // Not valid so don't default to it anyway.
                    if (!IsValid(profile, session, defaultLoadout.Prototype, collection, out _))
                        continue;

                    loadouts.Add(defaultLoadout);
                    Apply(loadoutProto);
                }
            }

            SelectedLoadouts[group] = loadouts;
        }

        foreach (var value in groupRemove)
        {
            SelectedLoadouts.Remove(value);
        }

        // ADT SAI Custom start
        // Extras validation (we don't want assists to have SAI data, right?)
        if (!protoManager.TryIndex(Role, out var role) || role.AllowedExtras == null)
        {
            ExtraData.Clear();
            return;
        }

        List<string> toRemove = new();
        foreach (var extra in ExtraData)
        {
            if (role.AllowedExtras.Contains(extra.Key))
                continue;
            toRemove.Add(extra.Key);
        }
        foreach (var key in toRemove)
        {
            ExtraData.Remove(key);
        }
        // ADT SAI Custom end
    }

    private void Apply(LoadoutPrototype loadoutProto)
    {
        foreach (var effect in loadoutProto.Effects)
        {
            effect.Apply(this);
        }
    }

    /// <summary>
    /// Resets the selected loadouts to default if no data is present.
    /// </summary>
    public void SetDefault(HumanoidCharacterProfile? profile, ICommonSession? session, IPrototypeManager protoManager, bool force = false)
    {
        if (profile == null)
            return;

        if (force)
        {
            SelectedLoadouts.Clear();
            ExtraData.Clear();  // ADT SAI Custom
        }

        var collection = IoCManager.Instance!;
        var roleProto = protoManager.Index(Role);

        for (var i = roleProto.Groups.Count - 1; i >= 0; i--)
        {
            var group = roleProto.Groups[i];

            if (!protoManager.TryIndex(group, out var groupProto))
                continue;

            if (SelectedLoadouts.ContainsKey(group))
                continue;

            var loadouts = new List<Loadout>();
            SelectedLoadouts[group] = loadouts;

            if (groupProto.MinLimit > 0)
            {
                // Apply any loadouts we can.
                foreach (var protoId in groupProto.Loadouts)
                {
                    // Reached the limit, time to stop
                    if (loadouts.Count >= groupProto.MinLimit)
                        break;

                    if (!protoManager.TryIndex(protoId, out var loadoutProto))
                        continue;

                    var defaultLoadout = new Loadout()
                    {
                        Prototype = loadoutProto.ID,
                    };

                    // Not valid so don't default to it anyway.
                    if (!IsValid(profile, session, defaultLoadout.Prototype, collection, out _))
                        continue;

                    loadouts.Add(defaultLoadout);
                    Apply(loadoutProto);
                }
            }
        }
    }

    /// <summary>
    /// Returns whether a loadout is valid or not.
    /// </summary>
    public bool IsValid(HumanoidCharacterProfile profile, ICommonSession? session, ProtoId<LoadoutPrototype> loadout, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        var protoManager = collection.Resolve<IPrototypeManager>();

        if (!protoManager.TryIndex(loadout, out var loadoutProto))
        {
            // Uhh
            reason = FormattedMessage.FromMarkupOrThrow("");
            return false;
        }

        if (!protoManager.HasIndex(Role))
        {
            reason = FormattedMessage.FromUnformatted("loadouts-prototype-missing");
            return false;
        }

        var valid = true;

        foreach (var effect in loadoutProto.Effects)
        {
            valid = valid && effect.Validate(profile, this, session, collection, out reason);
        }

        return valid;
    }

    /// <summary>
    /// Applies the specified loadout to this group.
    /// </summary>
    public bool AddLoadout(ProtoId<LoadoutGroupPrototype> selectedGroup, ProtoId<LoadoutPrototype> selectedLoadout, IPrototypeManager protoManager)
    {
        // Ganimed sponsor start
        if (!SelectedLoadouts.TryGetValue(selectedGroup, out var groupLoadouts))
            return false;

        var newProto = protoManager.Index(selectedLoadout);

        var conflictingGroups = new List<ProtoId<LoadoutGroupPrototype>>();
        foreach (var (groupId, items) in SelectedLoadouts)
        {
            if (!protoManager.TryIndex(groupId, out var groupProto))
                continue;

            foreach (var item in items)
            {
                if (!protoManager.TryIndex(item.Prototype, out var otherProto))
                    continue;

                if (Conflicts(newProto, otherProto))
                {
                    conflictingGroups.Add(groupId);
                    break;
                }
            }
        }

        foreach (var group in conflictingGroups)
        {
            if (!SelectedLoadouts.TryGetValue(group, out var items))
                continue;

            items.Clear();
        }
        // Ganimed sponsor end

        groupLoadouts.Add(new Loadout()
        {
            Prototype = selectedLoadout,
        });

        return true;
    }

    // Ganimed sponsor start
    private bool Conflicts(LoadoutPrototype a, LoadoutPrototype b)
    {
        foreach (var slot in a.Equipment.Keys)
        {
            if (b.Equipment.ContainsKey(slot))
                return true;
        }

        return false;
    }
    // Ganimed sponsor end

    public bool RemoveLoadout(ProtoId<LoadoutGroupPrototype> selectedGroup, ProtoId<LoadoutPrototype> selectedLoadout, IPrototypeManager protoManager)
    {
        if (!SelectedLoadouts.TryGetValue(selectedGroup, out var groupLoadouts)) // Ganimed sponsor
            return false;

        for (var i = 0; i < groupLoadouts.Count; i++)
        {
            // Ganimed sponsor start
            if (groupLoadouts[i].Prototype == selectedLoadout)
            {
                groupLoadouts.RemoveAt(i);
                return true;
            }
            // Ganimed sponsor end
        }

        return false;
    }

    public bool Equals(RoleLoadout? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        if (!Role.Equals(other.Role) ||
            SelectedLoadouts.Count != other.SelectedLoadouts.Count ||
            Points != other.Points ||
            EntityName != other.EntityName)
        {
            return false;
        }

        // Tried using SequenceEqual but it stinky so.
        foreach (var (key, value) in SelectedLoadouts)
        {
            if (!other.SelectedLoadouts.TryGetValue(key, out var otherValue) ||
                !otherValue.SequenceEqual(value))
            {
                return false;
            }
        }

        // ADT SAI Custom start
        if (!ExtraData.SequenceEqual(other.ExtraData))
            return false;
        // ADT SAI Custom end

        return true;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is RoleLoadout other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Role, SelectedLoadouts, Points);
    }
}
