namespace Content.Server.ADT.SpeechBarks;

public sealed class TransformSpeakerBarkEvent : EntityEventArgs
{
    public EntityUid Sender;
    public string Sound;
    public float Pitch;

    public TransformSpeakerBarkEvent(EntityUid sender, string sound, float pitch)
    {
        Sender = sender;
        Sound = sound;
        Pitch = pitch;
    }
}
