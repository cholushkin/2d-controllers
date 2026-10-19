using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // A plain bounds check instead of a physics sensor shape - keeps "am I in water"
    // off the low-level physics world and avoids relying on unconfirmed sensor APIs.
    [DisallowMultipleComponent]
    public sealed class WaterVolume : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new(10f, 5f);

        private static readonly List<WaterVolume> ActiveVolumes = new();

        private void OnEnable() => ActiveVolumes.Add(this);
        private void OnDisable() => ActiveVolumes.Remove(this);

        private Rect Bounds
        {
            get
            {
                Vector2 center = transform.position;
                return new Rect(center - size * 0.5f, size);
            }
        }

        public bool TryGetSurfaceY(Vector2 position, out float surfaceY)
        {
            var bounds = Bounds;
            if (position.x < bounds.xMin || position.x > bounds.xMax || position.y > bounds.yMax || position.y < bounds.yMin)
            {
                surfaceY = 0f;
                return false;
            }

            surfaceY = bounds.yMax;
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);
            Gizmos.DrawCube(transform.position, size);
        }
    }
}
