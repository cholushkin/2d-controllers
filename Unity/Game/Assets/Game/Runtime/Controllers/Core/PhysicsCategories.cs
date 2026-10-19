using UnityEngine.LowLevelPhysics2D;

namespace Game.Gameplay.Controllers.Core
{
    public static class PhysicsCategories
    {
        public static readonly PhysicsMask Player = new(1);
        public static readonly PhysicsMask Ground = new(2);
        public static readonly PhysicsMask Terrain = new(3);
        public static readonly PhysicsMask Debris = new(4);
        public static readonly PhysicsMask Projectile = new(5);
    }
}
