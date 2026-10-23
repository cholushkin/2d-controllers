

namespace Game.Gameplay.Controllers.Core
{
    public static class PhysicsCategories
    {
        public static readonly Unity.U2D.Physics.PhysicsMask Player = new(1);
        public static readonly Unity.U2D.Physics.PhysicsMask Ground = new(2);
        public static readonly Unity.U2D.Physics.PhysicsMask Terrain = new(3);
        public static readonly Unity.U2D.Physics.PhysicsMask Debris = new(4);
        public static readonly Unity.U2D.Physics.PhysicsMask Projectile = new(5);
    }
}
