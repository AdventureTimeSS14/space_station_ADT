using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.Tabletop;

namespace Content.Server.OuijaBoard;

[UsedImplicitly]
public sealed partial class OuijaBoardSetup : TabletopSetup
{

    [DataField("prototypePiece", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PrototypePiece = default!;

    public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
    {
        session.Entities.Add(
            entityManager.SpawnEntity(BoardPrototype, session.Position.Offset(0, 0))
        );

        entityManager.SpawnEntity(PrototypePiece, session.Position.Offset(-1, 0));
    }
}
