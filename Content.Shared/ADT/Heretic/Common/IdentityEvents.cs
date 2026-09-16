namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob Common.Identity

[ByRefEvent]
public record struct GetIdentityRepresentationEntityEvent(EntityUid? Uid = null);
