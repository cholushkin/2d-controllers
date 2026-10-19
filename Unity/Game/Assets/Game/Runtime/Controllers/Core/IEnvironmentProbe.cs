using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    public interface IEnvironmentProbe
    {
        bool IsGrounded(Vector2 position, float checkRadius);
        bool IsSubmerged(Vector2 position);
        bool TryGetRockAhead(Vector2 position, Vector2 direction, float probeDistance, out Vector2 hitPoint);
    }
}
