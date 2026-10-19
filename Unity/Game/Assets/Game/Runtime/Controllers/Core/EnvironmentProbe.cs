using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

namespace Game.Gameplay.Controllers.Core
{
    // Deliberately built only on PhysicsWorld.OverlapPoint (the one query signature
    // confirmed against the in-repo destructor reference) rather than CastRay/CastShape,
    // whose exact overloads were not verified against this engine build.
    public sealed class EnvironmentProbe : IEnvironmentProbe
    {
        private readonly PhysicsWorld _world;

        public EnvironmentProbe(PhysicsWorld world)
        {
            _world = world;
        }

        public bool IsGrounded(Vector2 position, float checkRadius)
        {
            var mask = PhysicsCategories.Ground | PhysicsCategories.Terrain;
            return OverlapPoint(position + Vector2.down * checkRadius, mask)
                || OverlapPoint(position + new Vector2(-checkRadius, -checkRadius * 0.5f), mask)
                || OverlapPoint(position + new Vector2(checkRadius, -checkRadius * 0.5f), mask);
        }

        public bool IsSubmerged(Vector2 position)
        {
            return WaterVolume.TryFindSurface(position, out var surfaceY) && position.y <= surfaceY;
        }

        public bool TryGetRockAhead(Vector2 position, Vector2 direction, float probeDistance, out Vector2 hitPoint)
        {
            hitPoint = position + direction.normalized * probeDistance;
            return OverlapPoint(hitPoint, PhysicsCategories.Terrain);
        }

        private bool OverlapPoint(Vector2 point, PhysicsMask mask)
        {
            using var hits = _world.OverlapPoint(point, new PhysicsQuery.QueryFilter { categories = mask, hitCategories = mask });
            return hits.Length > 0;
        }
    }
}
