// There isn't really a 'default place' to put these,
// so a file in the project top level directory it is

global using System;
global using System.Collections.Generic;
global using Robust.Shared.Analyzers;
global using Robust.Shared.Log;
global using Robust.Shared.Localization;
global using Robust.Shared.GameObjects;
global using Robust.Shared.IoC;
global using Robust.Shared.Maths;
global using Robust.Shared.ViewVariables;
global using Robust.Shared.Serialization.Manager.Attributes;
global using Content.Shared.Ghost.Components; // ADT-Tweak: GhostComponent moved namespaces

// ADT-Tweak: upstream renamed these shared systems to sealed types.
global using SharedBloodstreamSystem = Content.Shared.Body.Systems.BloodstreamSystem;
global using SharedRatvarianLanguageSystem = Content.Shared.Speech.EntitySystems.RatvarianLanguageSystem;


