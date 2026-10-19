using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

namespace Game.Gameplay.Controllers.Core
{
    // Shared, non-reactive data passed to every state. Mode-specific collaborators
    // (aim line view, terrain, shooting module, ...) are injected directly into the
    // states/processors that need them instead of being piled on here.
    public sealed class ControllerContext
    {
        public PhysicsBody Body { get; }
        public Transform Transform { get; }
        public PhysicsWorld World { get; }
        public IEnvironmentProbe Probe { get; }
        public IControllerInput Input { get; }
        public EnergyModel Energy { get; }

        public InputSample CurrentInput { get; private set; }

        public ControllerContext(PhysicsBody body, Transform transform, PhysicsWorld world,
            IEnvironmentProbe probe, IControllerInput input, EnergyModel energy)
        {
            Body = body;
            Transform = transform;
            World = world;
            Probe = probe;
            Input = input;
            Energy = energy;
        }

        public void SampleInput() => CurrentInput = Input != null ? Input.Sample() : default;
    }
}
