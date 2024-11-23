using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Server.ADT.SS40k.Turrets.TurretController;

[RegisterComponent]
public sealed partial class TurretControllerComponent : Component
{
    [ViewVariables]
    public EntityUid? CurrentUser;

    [ViewVariables]
    public EntityUid? CurrentTurret; // ??? maybe uberu nahui (potom optimiziruye, skoreye vsego legche save component turret v controllere(maybe more than 1 turret so i will make some changes after, IDK))
}

