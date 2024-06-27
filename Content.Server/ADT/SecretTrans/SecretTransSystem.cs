

namespace Content.Server.ADT.SecretTrans
{
    public sealed class SecretTransSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SecretTransComponent, ComponentInit>(OnComponentInit);

        }

        private void OnComponentInit(EntityUid uid, SecretTransComponent component, ComponentInit args)
        {
            ///that system removes proto and spawns proto from a secret repo
            ///the commented code shoud be ONLY on a secret repo
            //var cords = Transform(uid).Coordinates;
            //Spawn(component.Proto, cords);
            //QueueDel(uid);
        }
    }
}
