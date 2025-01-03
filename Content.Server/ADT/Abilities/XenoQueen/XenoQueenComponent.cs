using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Abilities.XenoQueen
{
    /// <summary>
    /// Lets its owner entity use mime powers, like placing invisible walls.
    /// </summary>
    [RegisterComponent]
    public sealed partial class XenoQueenComponent : Component
    {
        [DataField]
        public bool XenoCreatTurretEnabled = true;

        // 
        [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string XenoTurret = "WeaponTurretXeno";

        [DataField("xenoTurretAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? XenoTurretAction = "ActionXenoQueenTurret";

        // Призывы                    
        public EntityUid? XenoTurretActionEntity;
        public EntityUid? ActionSpawnXenoBurrower;
        public EntityUid? ActionSpawnXenoDrone;
        public EntityUid? ActionSpawnXenoRunner;
        public EntityUid? ActionSpawnXenoSpitter;
        public EntityUid? ActionSpawnXenoPraetorian;
        public EntityUid? ActionSpawnXenoRavager;
        public EntityUid? ActionSpawnXenoQueen;

        // Регенрация очков
        [DataField]
        public bool Regenetarion = true; // Можно ли регенерировать очки.

        [DataField]
        public float RegenDelay = 60f; // Секунды до регена. Используется в счетчике

        [ViewVariables]
        public float Accumulator = 0f; // Сам счетчик 0.000000

        [DataField]
        public FixedPoint2 BloobCount = 20; // Очки. Начальные очки равны 20

        [DataField]
        public FixedPoint2 MaxBloobCount = 150; // Максимальыне количество очков

        [DataField]
        public int RegenBloobCount = 3; // Реген очков в минуту
    }
}
