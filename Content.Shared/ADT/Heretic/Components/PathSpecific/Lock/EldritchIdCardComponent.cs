//

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using System.Linq;

namespace Content.Shared.Heretic.Components.PathSpecific.Lock;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class EldritchIdCardComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EldritchIdConfiguration> Configs = new();

    [DataField, AutoNetworkedField]
    public EntProtoId? CurrentProto;

    [DataField, AutoNetworkedField]
    public bool Inverted;

    [DataField, AutoNetworkedField]
    public EntityUid? PortalOne;

    [DataField, AutoNetworkedField]
    public EntityUid? PortalTwo;

    [DataField]
    public EntProtoId Portal = "LockPortal";

    [DataField]
    public SoundSpecifier EatSound = new SoundPathSpecifier("/Audio/ADT/Heretic/eatfood.ogg");

    [DataField]
    public TimeSpan PortalCreationTime = TimeSpan.FromSeconds(1.5);
}

[Serializable, NetSerializable]
public sealed class EldritchIdConfiguration(
    string? fullName,
    string? jobTitle,
    ProtoId<JobIconPrototype> jobIcon,
    List<ProtoId<DepartmentPrototype>> departments,
    EntProtoId cardPrototype)
{
    public readonly string? FullName = fullName;
    public readonly string? JobTitle = jobTitle;
    public readonly ProtoId<JobIconPrototype> JobIcon = jobIcon;
    public readonly List<ProtoId<DepartmentPrototype>> Departments = departments;
    public readonly EntProtoId CardPrototype = cardPrototype;

    public override bool Equals(object? obj)
    {
        if (obj is not EldritchIdConfiguration other)
            return false;

        return FullName == other.FullName &&
               JobTitle == other.JobTitle &&
               JobIcon.Id == other.JobIcon.Id &&
               CardPrototype.Id == other.CardPrototype.Id &&
               Departments.SequenceEqual(other.Departments);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FullName);
        hash.Add(JobTitle);
        hash.Add(JobIcon.Id);
        hash.Add(CardPrototype.Id);
        hash.Add(Departments.Count);
        foreach (var dept in Departments)
        {
            hash.Add(dept);
        }
        return hash.ToHashCode();
    }
}
