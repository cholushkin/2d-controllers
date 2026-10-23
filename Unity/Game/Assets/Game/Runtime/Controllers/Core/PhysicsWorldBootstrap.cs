using UnityEngine;


namespace Game.Gameplay.Controllers.Core
{
    // Configures the shared low-level world once. World/Probe are static accessors
    // rather than a singleton MonoBehaviour reference, so controllers never depend on
    // this component's Awake order - PhysicsWorld.defaultWorld is always valid.
    [DefaultExecutionOrder(-1000)]
    public sealed class PhysicsWorldBootstrap : MonoBehaviour
    {
        [SerializeField] private float gravityMultiplier = 1f;

        private static IEnvironmentProbe _probe;
        private static bool _configured;

        public static Unity.U2D.Physics.PhysicsWorld World => Unity.U2D.Physics.PhysicsWorld.defaultWorld;
        public static IEnvironmentProbe Probe => _probe ??= new EnvironmentProbe(World);

        private void Awake()
        {
            if (_configured)
                return;

            var world = World;
            world.gravity *= gravityMultiplier;
            world.simulationType = Unity.U2D.Physics.PhysicsWorld.SimulationType.FixedUpdate;
            _configured = true;
        }
    }
}
