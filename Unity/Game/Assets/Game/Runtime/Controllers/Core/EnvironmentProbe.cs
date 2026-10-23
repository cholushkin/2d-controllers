using UnityEngine;


namespace Game.Gameplay.Controllers.Core
{
    // Deliberately built only on PhysicsWorld.OverlapPoint (the one query signature
    // confirmed against the in-repo destructor reference) rather than CastRay/CastShape,
    // whose exact overloads were not verified against this engine build.
    public sealed class EnvironmentProbe : IEnvironmentProbe
    {
        private readonly Unity.U2D.Physics.PhysicsWorld _world;

        public EnvironmentProbe(Unity.U2D.Physics.PhysicsWorld world)
        {
            _world = world;
        }

        public bool IsGrounded(Vector2 position, float checkRadius)
        {
            var mask = PhysicsCategories.Ground | PhysicsCategories.Terrain;
            var (a, b, c) = GetGroundProbePoints(position, checkRadius);
            return OverlapPoint(a, mask) || OverlapPoint(b, mask) || OverlapPoint(c, mask);
        }

        public (Vector2 a, Vector2 b, Vector2 c) GetGroundProbePoints(Vector2 position, float checkRadius) => (
            position + Vector2.down * checkRadius,
            position + new Vector2(-checkRadius, -checkRadius * 0.5f),
            position + new Vector2(checkRadius, -checkRadius * 0.5f));

        public bool IsSubmerged(Vector2 position)
        {
            return WaterVolume.TryFindSurface(position, out var surfaceY) && position.y <= surfaceY;
        }

        public bool TryGetRockAhead(Vector2 position, Vector2 direction, float probeDistance, out Vector2 hitPoint)
        {
            hitPoint = position + direction.normalized * probeDistance;
            return OverlapPoint(hitPoint, PhysicsCategories.Terrain);
        }

        private bool OverlapPoint(Vector2 point, Unity.U2D.Physics.PhysicsMask mask)
        {
            using var hits = _world.OverlapPoint(point, new Unity.U2D.Physics.PhysicsQuery.QueryFilter { categories = mask, hitCategories = mask });
            return hits.Length > 0;
        }
    }
}
