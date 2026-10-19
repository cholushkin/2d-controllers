using Game.Gameplay.Controllers.Core;
using Game.Gameplay.Controllers.Director;
using Game.Gameplay.Controllers.Terrain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevelPhysics2D;

namespace Game.Gameplay.Controllers.Capsule
{
    [DisallowMultipleComponent]
    public sealed class CapsuleController : MonoBehaviour
    {
        [SerializeField] private CapsuleConfig config;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private AimLineView aimLine;
        [SerializeField] private DestructibleTerrain terrain;

        private PhysicsBody _body;
        private ControllerContext _context;
        private ControllerStateMachine<CapsuleStateId> _machine;
        private EnergyModel _energy;
        private IEnvironmentProbe _probe;
        private JumpHoldGesture _jumpGesture;
        private TimeFreezeController _timeFreeze;
        private bool _inputEnabled = true;

        public EnergyModel Energy => _energy;
        public CapsuleStateId CurrentState => _machine.CurrentStateId;

        private void Awake()
        {
            var world = PhysicsWorldBootstrap.World;
            _probe = PhysicsWorldBootstrap.Probe;

            _body = LowLevelBody.CreateCircleBody(world, transform, config.bodyRadius,
                PhysicsBody.BodyType.Dynamic, PhysicsCategories.Player,
                PhysicsCategories.Ground | PhysicsCategories.Terrain | PhysicsCategories.Debris,
                gravityScale: 1f);

            _energy = new EnergyModel(config.maxBatteries, config.maxEnergy);
            _timeFreeze = new TimeFreezeController();
            _jumpGesture = new JumpHoldGesture(config.jumpHoldThreshold);

            var spawner = new PlasmaBulletSpawner();
            var shooting = new ShootingModule(config.shoot, aimLine, spawner);

            var input = new PlayerInputReader(inputActions);
            _context = new ControllerContext(_body, transform, world, _probe, input, _energy);

            var states = new IControllerState<CapsuleStateId>[]
            {
                new CapsuleGroundState(config.ground, shooting),
                new CapsuleUnderwaterState(config.underwater, shooting),
                new CapsuleJumpAimingState(config.jumpAim, aimLine, _timeFreeze, OnJumpReleased),
                new CapsuleDrillState(config.drill, terrain)
            };

            var initial = _probe.IsSubmerged(transform.position) ? CapsuleStateId.Underwater : CapsuleStateId.Ground;
            _machine = new ControllerStateMachine<CapsuleStateId>(_context, states, initial);
        }

        public void SetInputEnabled(bool isEnabled) => _inputEnabled = isEnabled;

        private void OnJumpReleased()
        {
            var submerged = _probe.IsSubmerged(_body.position);
            _machine.RequestTransition(submerged ? CapsuleStateId.Underwater : CapsuleStateId.Ground);
        }

        private void Update()
        {
            _timeFreeze.Tick();

            if (!_inputEnabled)
                return;

            _context.SampleInput();
            EvaluateTransitions();
            _machine.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_inputEnabled)
                _machine.FixedTick(Time.fixedDeltaTime);
        }

        private void EvaluateTransitions()
        {
            var current = _machine.CurrentStateId;
            var input = _context.CurrentInput;

            // Jump aiming freezes time, so unscaled delta time keeps the hold gesture responsive.
            _jumpGesture.Tick(input, Time.unscaledDeltaTime);

            if (current == CapsuleStateId.Ground || current == CapsuleStateId.Underwater)
            {
                if (_jumpGesture.LightHopRequested && current == CapsuleStateId.Ground)
                    _body.ApplyLinearImpulseToCenter(Vector2.up * config.ground.jumpImpulse, true);

                if (_jumpGesture.AimedJumpRequested)
                {
                    _machine.RequestTransition(CapsuleStateId.JumpAiming);
                    return;
                }

                var drillDirection = input.Move.sqrMagnitude > 0.01f ? input.Move.normalized : Vector2.down;
                if (input.DrillPressed && _probe.TryGetRockAhead(_body.position, drillDirection, config.drill.probeDistance, out _))
                {
                    _machine.RequestTransition(CapsuleStateId.Drilling);
                    return;
                }

                var submerged = _probe.IsSubmerged(_body.position);
                var desired = submerged ? CapsuleStateId.Underwater : CapsuleStateId.Ground;
                if (desired != current)
                    _machine.RequestTransition(desired);
            }
            else if (current == CapsuleStateId.Drilling && input.DrillReleased)
            {
                var submerged = _probe.IsSubmerged(_body.position);
                _machine.RequestTransition(submerged ? CapsuleStateId.Underwater : CapsuleStateId.Ground);
            }
        }

        private void OnDestroy()
        {
            if (_body.isValid)
                _body.Destroy();
        }
    }
}
