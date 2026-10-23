using System;
using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // One asset holding all capsule mode settings (simpler to author than many
    // separate ScriptableObjects); each nested section still maps 1:1 to a state/module.
    [CreateAssetMenu(menuName = "Controllers/Capsule Config", fileName = "CapsuleConfig")]
    public sealed class CapsuleConfig : ScriptableObject
    {
        [Serializable]
        public sealed class GroundSettings
        {
            public float rollTorque = 12f;
            public float maxAngularSpeed = 12f;
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
            public float holdThreshold = 0.15f;
            public float hopImpulse = 4f;
            public float minPowerThreshold = 0.1f;
            public float maxImpulse = 10f;
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
            public float holdThreshold = 0.15f;
            public float fireInterval = 0.2f;
            public float baseRadius = 0.1f;
            public float baseSpeed = 14f;
            public float radiusGrowth = 0.05f;
            public float speedGrowth = 2f;
            public int maxChargeSteps = 5;
            public float bulletLifetime = 2.5f;
        }

        [Serializable]
        public sealed class ShieldSettings
        {
            public float radius = 1.2f;
            public float duration = 1.5f;
            public float reflectSpeedBonus = 4f;
        }

        [Serializable]
        public sealed class BoostSettings
        {
            public float moveMultiplier = 1.8f;
            public float dampingMultiplier = 0.5f;
            public float energyDrainPerSecond = 0.1f;
        }

        public float bodyRadius = 0.5f;
        public int maxBatteries = 3;
        public float maxEnergy = 1f;

        public GroundSettings ground = new();
        public UnderwaterSettings underwater = new();
        public JumpAimSettings jumpAim = new();
        public DrillSettings drill = new();
        public ShootSettings shoot = new();
        public ShieldSettings shield = new();
        public BoostSettings boost = new();
        public FreezeSettings freeze = new();
    }
}
