namespace Enemies.FunkyPhysics
{
    public class TankPhysics : BasePhysics
    {
        protected override void Start()
        {
            base.Start();
            
            Rb.freezeRotation = false;
        }
    }
}