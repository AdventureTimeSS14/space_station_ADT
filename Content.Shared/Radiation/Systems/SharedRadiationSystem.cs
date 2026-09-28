<<<<<<< HEAD
using Content.Shared.Radiation.Components;
=======
﻿using Content.Shared.Radiation.Components;
>>>>>>> wizards-filtered

namespace Content.Shared.Radiation.Systems;

public abstract partial class SharedRadiationSystem : EntitySystem
{
<<<<<<< HEAD
    [Dependency] protected readonly EntityQuery<RadiationSourceComponent> SourceQuery = default!;
=======
    [Dependency] protected EntityQuery<RadiationSourceComponent> SourceQuery = default!;
>>>>>>> wizards-filtered

    /// <summary>
    /// Sets the intensity of a <see cref="RadiationSourceComponent"/> to the passed intensity.
    /// </summary>
    /// <param name="entity">Radiation source we're attempting to update</param>
    /// <param name="intensity">Intensity we're setting the source to.</param>
    public void SetIntensity(Entity<RadiationSourceComponent?> entity, float intensity)
    {
        if (!SourceQuery.Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.Intensity = intensity;
    }
<<<<<<< HEAD

    // ADT-Tweak start
    public void SetSlope(Entity<RadiationSourceComponent?> entity, float slope)
    {
        if (!SourceQuery.Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.Slope = slope;
    }

    // ADT-Tweak end
}
=======
}
>>>>>>> wizards-filtered
