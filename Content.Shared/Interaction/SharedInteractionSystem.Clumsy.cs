// using Content.Shared.Interaction.Components;
// using Robust.Shared.Random;
// using Content.Shared.Roles; // ADT-Clumsy-Tweak
// using Content.Shared.Mind;  // ADT-Clumsy-Tweak
// ADT COmment
// namespace Content.Shared.Interaction
// {
//     public partial class SharedInteractionSystem
//     {
//         [Dependency] private readonly SharedRoleSystem _role = default!; // ADT-Clumsy-Tweak
//         [Dependency] private readonly SharedMindSystem _mind = default!; // ADT-Clumsy-Tweak

//         // ADT-Clumsy-Tweak-Start
//         public bool GetAntagonistStatus(EntityUid uid, ClumsyComponent component)
//         {
//             var mindId = _mind.GetMind(uid);
//             if (mindId == null)
//                 return false;

//             return _role.MindIsAntagonist(mindId);
//         }
//         // ADT-Clumsy-Tweak-End
//         public bool RollClumsy(ClumsyComponent component, float chance)
//         {
//             return component.Running && _random.Prob(chance);
//         }

//         /// <summary>
//         ///     Rolls a probability chance for a "bad action" if the target entity is clumsy.
//         /// </summary>
//         /// <param name="entity">The entity that the clumsy check is happening for.</param>
//         /// <param name="chance">
//         /// The chance that a "bad action" happens if the user is clumsy, between 0 and 1 inclusive.
//         /// </param>
//         /// <returns>True if a "bad action" happened, false if the normal action should happen.</returns>
//         public bool TryRollClumsy(EntityUid entity, float chance, ClumsyComponent? component = null)
//         {
//             return Resolve(entity, ref component, false) && RollClumsy(component, chance);
//         }
//     }
// }
