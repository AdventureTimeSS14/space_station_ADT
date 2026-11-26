using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.ADT.Virology;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class ADTVirusData : ReagentData
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<ADTDisease> Diseases = new();

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string> Resistances = new();

    [ViewVariables]
    public string DiseaseIds => string.Join(", ", Diseases.Select(d => d.GetId()));

    public override ReagentData Clone()
    {
        return new ADTVirusData
        {
            Diseases = Diseases.Select(d => d.Copy()).ToList(),
            Resistances = new HashSet<string>(Resistances),
        };
    }

    public override bool Equals(ReagentData? other)
    {
        if (other is not ADTVirusData data)
            return false;

        if (!Resistances.SetEquals(data.Resistances))
            return false;

        if (Diseases.Count != data.Diseases.Count)
            return false;

        var ids = Diseases.Select(d => d.GetId()).OrderBy(i => i, StringComparer.Ordinal);
        var otherIds = data.Diseases.Select(d => d.GetId()).OrderBy(i => i, StringComparer.Ordinal);
        return ids.SequenceEqual(otherIds);
    }

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var id in Diseases.Select(d => d.GetId()).OrderBy(i => i, StringComparer.Ordinal))
        {
            hash = hash * 31 + id.GetHashCode();
        }

        return hash;
    }
}

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class ADTVaccineData : ReagentData
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string> Ids = new();

    [ViewVariables]
    public string VaccineIds => string.Join(", ", Ids);

    public override ReagentData Clone()
    {
        return new ADTVaccineData
        {
            Ids = new HashSet<string>(Ids),
        };
    }

    public override bool Equals(ReagentData? other)
    {
        return other is ADTVaccineData data && Ids.SetEquals(data.Ids);
    }

    public override int GetHashCode()
    {
        var hash = 19;
        foreach (var id in Ids.OrderBy(i => i, StringComparer.Ordinal))
        {
            hash = hash * 31 + id.GetHashCode();
        }

        return hash;
    }
}
