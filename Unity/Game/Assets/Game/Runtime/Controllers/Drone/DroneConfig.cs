using System;
using UnityEngine;

namespace Game.Gameplay.Controllers.Drone
{
    [CreateAssetMenu(menuName = "Controllers/Drone Config", fileName = "DroneConfig")]
    public sealed class DroneConfig : ScriptableObject
    {
        [Serializable]
        public sealed class FlightSettings
        {
            public float thrustForce = 10f;
            public float maxSpeed = 6f;
            public float linearDamping = 1.5f;
            public float energyDrainPerSecond = 0.1f;
        }

        [Serializable]
        public sealed class LandingSettings
        {
            public float gravityScale = 1f;
            public float groundCheckRadius = 0.3f;
        }

        public float bodyRadius = 0.25f;
        public float maxEnergy = 1f;
        public FlightSettings flight = new();
        public LandingSettings landing = new();
    }
}
