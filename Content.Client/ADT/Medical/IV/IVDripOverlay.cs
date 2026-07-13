// TODO: Добавить оверлей для капельницы, добавляет нитку от капельницы к пациенту. https://github.com/RMC-14/RMC-14
// using Content.Shared.ADT.Medical.IV;
// using Robust.Client.GameObjects;
// using Robust.Client.Graphics;
// using Robust.Shared.Enums;
// using Robust.Shared.Map;
//
// namespace Content.Client.ADT.Medical.IV;
//
// public sealed class IVDripOverlay : Overlay
// {
//     [Dependency] private readonly IEntityManager _entity = default!;
//
//     public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;
//
//     public IVDripOverlay()
//     {
//         IoCManager.InjectDependencies(this);
//     }
//
//     protected override void Draw(in OverlayDrawArgs args)
//     {
//         var transformSystem = _entity.System<TransformSystem>();
//         var handle = args.WorldHandle;
//
//         var ivDrips = _entity.EntityQueryEnumerator<IVDripComponent>();
//         while (ivDrips.MoveNext(out var ivDripId, out var ivDripComponent))
//         {
//             if (ivDripComponent.AttachedTo is not { Valid: true } attachedTo)
//             {
//                 continue;
//             }
//
//             var ivDripPosition = transformSystem.GetMapCoordinates(ivDripId);
//             var attachedPosition = transformSystem.GetMapCoordinates(attachedTo);
//
//             if (ivDripPosition.MapId == MapId.Nullspace || attachedPosition.MapId == MapId.Nullspace)
//                 continue;
//
//             handle.DrawLine(ivDripPosition.Position, attachedPosition.Position, Color.White);
//         }
//
//         var bloodPacks = _entity.EntityQueryEnumerator<BloodPackComponent>();
//         while (bloodPacks.MoveNext(out var packId, out var packComponent))
//         {
//             var attachedTo = packId;
//
//             var packPosition = transformSystem.GetMapCoordinates(packId);
//             var attachedPosition = transformSystem.GetMapCoordinates(attachedTo);
//
//             if (packPosition.MapId == MapId.Nullspace || attachedPosition.MapId == MapId.Nullspace)
//                 continue;
//
//             handle.DrawLine(packPosition.Position, attachedPosition.Position, Color.White);
//         }
//
//         var dialysisMachines = _entity.EntityQueryEnumerator<PortableDialysisComponent>();
//         while (dialysisMachines.MoveNext(out var dialysisId, out var dialysisComponent))
//         {
//             if (dialysisComponent.AttachedTo is not { Valid: true } attachedTo)
//                 continue;
//
//             var dialysisPosition = transformSystem.GetMapCoordinates(dialysisId);
//             var attachedPosition = transformSystem.GetMapCoordinates(attachedTo);
//
//             if (dialysisPosition.MapId == MapId.Nullspace || attachedPosition.MapId == MapId.Nullspace)
//                 continue;
//
//             handle.DrawLine(dialysisPosition.Position, attachedPosition.Position, Color.White);
//         }
//     }
// }
