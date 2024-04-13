// using System;
// using Robust.Shared.Animations;
// using Robust.Shared.GameStates;
// using Robust.Shared.Maths;
// using Robust.Shared.Serialization.Manager.Attributes;
// using Robust.Shared.ViewVariables;
// using System.Numerics;
// using Robust.Shared.IoC;

// namespace Content.Shared.ADT.PointLightColorBlue
// {
//     [NetworkedComponent]
//     public sealed partial class PointLightColorBlueComponent : Component
//     {
//         //[ViewVariables(VVAccess.ReadWrite)]
//         [DataField("color")]
//         [Animatable]
//         public Color Color { get; set; } = Color.Blue;

//         /// <summary>
//         /// Offset from the center of the entity.
//         /// </summary>
//         //[ViewVariables(VVAccess.ReadWrite)]
//         [DataField("offset")]
//         public Vector2 Offset = Vector2.Zero;

//         //[ViewVariables(VVAccess.ReadWrite)]
//         [DataField("energy")]
//         [Animatable]
//         public float Energy { get; set; } = 20f;

//         [DataField("softness"), Animatable]
//         public float Softness { get; set; } = 1f;

//         /// <summary>
//         ///     Whether this pointlight should cast shadows
//         /// </summary>
//         [DataField("castShadows")]
//         public bool CastShadows = true;

//         [DataField("enabled")]
//         public bool Enabled = true;

//         // TODO ECS animations
//         [Animatable]
//         public bool AnimatedEnable
//         {
//             [Obsolete]
//             get => Enabled;

//             [Obsolete]
//             set => IoCManager.Resolve<IEntityManager>().System<PointLightColorBlueSystem>().SetEnabled(Owner, value, this);
//         }

//         // TODO ECS animations
//         [Animatable]
//         public float AnimatedRadius
//         {
//             [Obsolete]
//             get => Radius;
//             [Obsolete]
//             set => IoCManager.Resolve<IEntityManager>().System<PointLightColorBlueSystem>().SetRadius(Owner, value, this);
//         }

//         /// <summary>
//         /// How far the light projects.
//         /// </summary>
//         [DataField("radius")]
//         public float Radius = 2f;

//         [ViewVariables]
//         public bool ContainerOccluded;

//         /// <summary>
//         ///     Determines if the light mask should automatically rotate with the entity. (like a flashlight)
//         /// </summary>
//         [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRot")]
//         public bool MaskAutoRotate;

//         /// <summary>
//         ///     Local rotation of the light mask around the center origin
//         /// </summary>
//         [ViewVariables(VVAccess.ReadWrite)]
//         [Animatable]
//         public Angle Rotation { get; set; }

//         /// <summary>
//         /// The resource path to the mask texture the light will use.
//         /// </summary>
//         [ViewVariables(VVAccess.ReadWrite)]
//         [DataField("mask")]
//         public string? MaskPath;
//     }

//     public sealed class PointLightColorBlueEvent : EntityEventArgs
//     {
//         public bool Enabled;

//         public PointLightColorBlueEvent(bool enabled)
//         {
//             Enabled = enabled;
//         }
//     }
// }
