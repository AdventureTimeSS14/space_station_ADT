namespace Content.Shared.ADT.Heretic.Common;

// ADT: перенесено из Content.Goobstation.Common.Identity

[ByRefEvent]
public record struct GetIdentityRepresentationEntityEvent(EntityUid? Uid = null);
