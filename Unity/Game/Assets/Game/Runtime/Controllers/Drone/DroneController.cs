using Game.Gameplay.Controllers.Core;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Game.Gameplay.Controllers.Drone
{
    [DisallowMultipleComponent]
    public sealed class DroneController : MonoBehaviour
    {
        [SerializeField] private DroneConfig config;
        [SerializeField] private InputActionAsset inputActions;

        private Unity.U2D.Physics.PhysicsBody _body;
        private ControllerContext _context;
        private ControllerStateMachine<DroneStateId> _machine;
        private IEnvironmentProbe _probe;
        private EnergyModel _energy;
        private bool _active;

        public DroneStateId CurrentState => _machine.CurrentStateId;
        public Vector2 Position => _body.position;

        private void Awake()
        {
            var world = PhysicsWorldBootstrap.World;
            _probe = PhysicsWorldBootstrap.Probe;

            _body = LowLevelBody.CreateCircleBody(world, transform, config.bodyRadius,
                Unity.U2D.Physics.PhysicsBody.BodyType.Dynamic, PhysicsCategories.Player,
                PhysicsCategories.Ground | PhysicsCategories.Terrain, gravityScale: 0f);

            _energy = new EnergyModel(0, config.maxEnergy);

            var input = new PlayerInputReader(inputActions);
            _context = new ControllerContext(_body, transform, world, _probe, input, _energy);

            var states = new IControllerState<DroneStateId>[]
            {
                new DroneFlyingState(config.flight),
                new DroneEmergencyLandingState(config.landing),
                new DroneDepletedState()
            };

            _machine = new ControllerStateMachine<DroneStateId>(_context, states, DroneStateId.Depleted);
            SetActive(false);
        }

        public void Launch()
        {
            _energy.ResetFull();
            SetActive(true);
            _machine.RequestTransition(DroneStateId.Flying);
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (_body.isValid)
                _body.enabled = active;
        }

        private void Update()
        {
            if (!_active)
                return;

            _context.SampleInput();

            if (_machine.CurrentStateId == DroneStateId.Flying && _energy.Energy <= 0f)
            {
                _machine.RequestTransition(DroneStateId.EmergencyLanding);
            }
            else if (_machine.CurrentStateId == DroneStateId.EmergencyLanding &&
                     _probe.IsGrounded(_body.position, config.landing.groundCheckRadius))
            {
                _machine.RequestTransition(DroneStateId.Depleted);
                SetActive(false);
            }

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
