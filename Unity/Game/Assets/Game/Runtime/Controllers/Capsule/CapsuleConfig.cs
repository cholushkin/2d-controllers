using System;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // One asset holding all capsule mode settings (simpler to author than five
    // separate ScriptableObjects); each nested section still maps 1:1 to a state.
    [CreateAssetMenu(menuName = "Controllers/Capsule Config", fileName = "CapsuleConfig")]
    public sealed class CapsuleConfig : ScriptableObject
    {
        [Serializable]
        public sealed class GroundSettings
        {
            public float rollTorque = 12f;
            public float maxAngularSpeed = 12f;
            public float jumpImpulse = 4f;
        }

        [Serializable]
        public sealed class UnderwaterSettings
        {
            public float gravityScale = 0.15f;
            public float linearDamping = 4f;
            public float moveForce = 6f;
            public float energyDrainPerSecond = 0.08f;
        }

        [Serializable]
        public sealed class JumpAimSettings
        {
            public float timeScaleWhileAiming = 0.05f;
            public float minPowerThreshold = 0.1f;
            public float maxImpulse = 10f;
            public float releaseOvershoot = 1.15f;
            public float timeScaleRecoverDuration = 0.12f;
        }

        [Serializable]
        public sealed class DrillSettings
        {
            public float drillSpeed = 2f;
            public float probeDistance = 0.6f;
            public float carveRadius = 0.6f;
        }

        [Serializable]
        public sealed class ShootSettings
        {
            public float fireInterval = 0.15f;
            public float bulletSpeed = 14f;
            public float bulletLifetime = 2.5f;
            public float bulletRadius = 0.1f;
        }

        public float bodyRadius = 0.5f;
        public int maxBatteries = 3;
        public float maxEnergy = 1f;
        public float jumpHoldThreshold = 0.15f;

        public GroundSettings ground = new();
        public UnderwaterSettings underwater = new();
        public JumpAimSettings jumpAim = new();
        public DrillSettings drill = new();
        public ShootSettings shoot = new();
    }
}
