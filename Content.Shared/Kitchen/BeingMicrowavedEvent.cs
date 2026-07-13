public sealed class BeingMicrowavedEvent(EntityUid microwave, EntityUid? user, bool beingHeated = false, bool beingIrradiated = false) : HandledEntityEventArgs
{
    // ADT-Tweak: fields for whether or not the object is actually being heated or irradiated.
    public bool BeingHeated = beingHeated;
    public bool BeingIrradiated = beingIrradiated;
    // End ADT-Tweak

    public EntityUid Microwave = microwave;
    public EntityUid? User = user;
}
