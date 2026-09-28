using Content.Shared.Objectives;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterInfo;

[Serializable, NetSerializable]
public sealed class RequestCharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public RequestCharacterInfoEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class CharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly ProtoId<JobPrototype>? Job;
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;
    public readonly string? Briefing;
    public readonly Dictionary<string, string> Memory; //ADT-Economy

<<<<<<< HEAD
    public CharacterInfoEvent(NetEntity netEntity, string jobTitle, Dictionary<string, List<ObjectiveInfo>> objectives, string? briefing, Dictionary<string, string> memory) //ADT-Economy
=======
    public CharacterInfoEvent(NetEntity netEntity, Dictionary<string, List<ObjectiveInfo>> objectives, string? briefing, ProtoId<JobPrototype>? job)
>>>>>>> wizards-filtered
    {
        NetEntity = netEntity;
        Objectives = objectives;
        Briefing = briefing;
<<<<<<< HEAD
        Memory = memory; //ADT-Economy
=======
        Job = job;
>>>>>>> wizards-filtered
    }
}
