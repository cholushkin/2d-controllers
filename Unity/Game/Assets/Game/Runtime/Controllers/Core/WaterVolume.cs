using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // Shape is authored in the editor via PhysicsPose + PhysicsAreaPolygon (Scene-view
    // box handles) instead of a typed-in size field. Water never needs to actually
    // simulate - Awake reads the authored geometry once into a plain Rect, then the
    // authoring components are discarded, keeping the original cheap bounds check.
    // Deliberately NOT [RequireComponent]: that would stop Awake's own Destroy() calls
    // below from ever running (Unity refuses to remove a component something else still
    // requires), so authoring is optional - no authored area falls back to fallbackSize.
    [DisallowMultipleComponent]
    public sealed class WaterVolume : MonoBehaviour
    {
        [SerializeField] private Vector2 fallbackSize = new(10f, 5f);

        private static readonly List<WaterVolume> ActiveVolumes = new();

        private Rect _bounds;

        private void Awake()
        {
            var area = GetComponent<Unity.U2D.Physics.PhysicsAreaPolygon>();
            _bounds = area != null
                ? ComputeWorldBounds(area.activeGeometry)
                : new Rect((Vector2)transform.position - fallbackSize * 0.5f, fallbackSize);

            if (area != null)
                Destroy(area);
            var pose = GetComponent<Unity.U2D.Physics.PhysicsPose>();
            if (pose != null)
                Destroy(pose);
        }

        private void OnEnable() => ActiveVolumes.Add(this);
        private void OnDisable() => ActiveVolumes.Remove(this);

        private Rect ComputeWorldBounds(Unity.U2D.Physics.PolygonGeometry geometry)
        {
            if (!geometry.isValid || geometry.count == 0)
                return new Rect(transform.position, Vector2.zero);

            var min = geometry.vertices[0];
            var max = geometry.vertices[0];
            for (var i = 1; i < geometry.count; i++)
            {
                min = Vector2.Min(min, geometry.vertices[i]);
                max = Vector2.Max(max, geometry.vertices[i]);
            }

            var origin = (Vector2)transform.position;
            return new Rect(origin + min, max - min);
        }

        public bool TryGetSurfaceY(Vector2 position, out float surfaceY)
        {
            if (position.x < _bounds.xMin || position.x > _bounds.xMax || position.y > _bounds.yMax || position.y < _bounds.yMin)
            {
                surfaceY = 0f;
                return false;
            }

            surfaceY = _bounds.yMax;
            return true;
        }

        public static bool TryFindSurface(Vector2 position, out float surfaceY)
        {
            foreach (var volume in ActiveVolumes)
            {
                if (volume.TryGetSurfaceY(position, out surfaceY))
                    return true;
            }

            surfaceY = 0f;
            return false;
        }

        // Reads the live authored shape directly so the box is visible (and accurate)
        // while editing it, before Awake ever converts it.
        private void OnDrawGizmos()
        {
            var area = GetComponent<Unity.U2D.Physics.PhysicsAreaPolygon>();
            var bounds = area != null ? ComputeWorldBounds(area.activeGeometry) : _bounds;

            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
