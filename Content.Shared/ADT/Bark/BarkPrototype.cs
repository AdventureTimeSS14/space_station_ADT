using Content.Shared.Humanoid;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SpeechBarks;

[Prototype("speechBark")]
public sealed partial class BarkPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public bool RoundStart = true;

    [DataField]
    public string Name = "Default";

    [DataField(required: true)]
    public string Sound = "/Audio/Voice/Talk/speak_1.ogg";
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class BarkData
{
    [DataField]
    public ProtoId<BarkPrototype> Proto = "Human1";

    /// <summary>
    /// Данное поле заполняется после инициализации барка, если не было задано в прототипе.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Sound = "";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinVar = 0.1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxVar = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Pitch = 1f;

    public BarkData WithProto(string proto)
    {
        var data = this;
        data.Proto = proto;
        return data;
    }

    public BarkData WithPitch(float pitch)
    {
        var data = this;
        data.Pitch = pitch;
        return data;
    }

    public BarkData WithMinVar(float var)
    {
        var data = this;
        data.MinVar = var;
        return data;
    }

    public BarkData WithMaxVar(float var)
    {
        var data = this;
        data.MaxVar = var;
        return data;
    }

    public BarkData(ProtoId<BarkPrototype> proto, float pitch, float minVar, float maxVar)
    {
        Proto = proto;
        Pitch = pitch;
        MinVar = minVar;
        MaxVar = maxVar;
    }

    public BarkData Copy()
    {
        return new BarkData()
        {
            Proto = Proto,
            Sound = Sound,
            Pitch = Pitch,
            MinVar = MinVar,
            MaxVar = MaxVar
        };
    }

    public bool MemberwiseEquals(BarkData other)
    {
        if (Proto != other.Proto)       return false;
        if (Sound != other.Sound)       return false;
        if (Pitch != other.Pitch)       return false;
        if (MinVar != other.MinVar)     return false;
        if (MaxVar != other.MaxVar)     return false;
        return true;
    }
}
