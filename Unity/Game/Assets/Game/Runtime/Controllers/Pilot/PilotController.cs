using Game.Gameplay.Controllers.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevelPhysics2D;

namespace Game.Gameplay.Controllers.Pilot
{
    [DisallowMultipleComponent]
    public sealed class PilotController : MonoBehaviour
    {
        [SerializeField] private PilotConfig config;
        [SerializeField] private InputActionAsset inputActions;

        private PhysicsBody _body;
        private ControllerContext _context;
        private ControllerStateMachine<PilotStateId> _machine;
        private IEnvironmentProbe _probe;
        private bool _active;

        private void Awake()
        {
            var world = PhysicsWorldBootstrap.World;
            _probe = PhysicsWorldBootstrap.Probe;

            _body = LowLevelBody.CreateCircleBody(world, transform, config.bodyRadius,
                PhysicsBody.BodyType.Dynamic, PhysicsCategories.Player,
                PhysicsCategories.Ground | PhysicsCategories.Terrain);

            var input = new PlayerInputReader(inputActions);
            var energy = new EnergyModel(0, 1f);
            _context = new ControllerContext(_body, transform, world, _probe, input, energy);

            var states = new IControllerState<PilotStateId>[]
            {
                new PilotGroundState(config.ground),
                new PilotSwimState(config.swim)
            };

            var initial = _probe.IsSubmerged(transform.position) ? PilotStateId.Underwater : PilotStateId.Ground;
            _machine = new ControllerStateMachine<PilotStateId>(_context, states, initial);

            SetActive(false);
        }

        // Toggles input/simulation only - the GameObject stays active in the hierarchy
        // so Awake (and body creation) always runs once at scene load.
        public void SetActive(bool active)
        {
            _active = active;
            if (_body.isValid)
            {
                _body.enabled = active;
                if (!active)
                    _body.linearVelocity = Vector2.zero;
            }
        }

        private void Update()
        {
            if (!_active)
                return;

            _context.SampleInput();

            var submerged = _probe.IsSubmerged(_body.position);
            _machine.RequestTransition(submerged ? PilotStateId.Underwater : PilotStateId.Ground);

            _machine.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_active)
                _machine.FixedTick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            if (_body.isValid)
                _body.Destroy();
        }
    }
}
