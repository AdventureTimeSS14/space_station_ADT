using Content.Shared.CrewManifest;
using Content.Shared.Roles;

namespace Content.Server.ADT.CrewManifest;

public readonly record struct CrewManifestEntriesCollectEvent(
    EntityUid Station,
    List<(JobPrototype? job, CrewManifestEntry entry)> Entries);
