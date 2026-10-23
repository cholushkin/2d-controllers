using System;
using UnityEngine;

namespace Game.Gameplay.Controllers.Pilot
{
    [CreateAssetMenu(menuName = "Controllers/Pilot Config", fileName = "PilotConfig")]
    public sealed class PilotConfig : ScriptableObject
    {
        [Serializable]
        public sealed class GroundSettings
        {
            public float moveSpeed = 4f;
            public float jumpImpulse = 5f;
        }

        [Serializable]
        public sealed class SwimSettings
        {
            public float gravityScale = 0.1f;
            public float linearDamping = 3f;
            public float moveForce = 4f;
        }

        public GroundSettings ground = new();
        public SwimSettings swim = new();
    }
}
