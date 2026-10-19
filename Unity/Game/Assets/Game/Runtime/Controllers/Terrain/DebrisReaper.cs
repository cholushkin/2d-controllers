using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

namespace Game.Gameplay.Controllers.Terrain
{
    // Explicit per-task requirement: debris does not collide with the player and
    // despawns after a fixed lifetime, rather than being removed on ground contact
    // (as in the Fragmenting.cs / DestructionController.cs references).
    public sealed class DebrisReaper : MonoBehaviour
    {
        [SerializeField] private FragmentMeshRenderer fragmentRenderer;

        private readonly List<(PhysicsBody body, float expireAt)> _scheduled = new();

        public void Register(PhysicsBody body, float lifetimeSeconds)
        {
            _scheduled.Add((body, Time.time + lifetimeSeconds));
        }

        private void Update()
        {
            for (var i = _scheduled.Count - 1; i >= 0; --i)
            {
                var (body, expireAt) = _scheduled[i];

                if (!body.isValid)
                {
                    _scheduled.RemoveAt(i);
                    continue;
                }

                if (Time.time < expireAt)
                    continue;

                fragmentRenderer?.Remove(body);
                body.Destroy();
                _scheduled.RemoveAt(i);
            }
        }
    }
}
