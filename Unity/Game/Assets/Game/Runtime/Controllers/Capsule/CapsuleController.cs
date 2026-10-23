using Game.Gameplay.Controllers.Core;
using Game.Gameplay.Controllers.Director;
using Game.Gameplay.Controllers.Terrain;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Game.Gameplay.Controllers.Capsule
{
    [DisallowMultipleComponent]
    public sealed class CapsuleController : MonoBehaviour
    {
        [SerializeField] private CapsuleConfig config;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private AimLineView aimLine;
        [SerializeField] private DestructibleTerrain terrain;

        private Unity.U2D.Physics.PhysicsBody _body;
        private ControllerContext _context;
        private ControllerStateMachine<CapsuleStateId> _machine;
        private EnergyModel _energy;
        private IEnvironmentProbe _probe;
        private TapHoldGesture _jumpGesture;
        private TapHoldGesture _fireGesture;
        private FreezeController _freeze;
        private ShootingModule _shooting;
        private ShieldModule _shield;
        private CapsuleJumpAimingState _jumpAimState;
        private CapsuleDrillState _drillState;
        private bool _inputEnabled = true;

        public EnergyModel Energy => _energy;
        public CapsuleStateId CurrentState => _machine.CurrentStateId;
        public DebugInfo DebugView => new(this);

        private void Awake()
        {
            var world = PhysicsWorldBootstrap.World;
            _probe = PhysicsWorldBootstrap.Probe;

            _body = LowLevelBody.CreateCircleBody(world, transform, config.bodyRadius,
                Unity.U2D.Physics.PhysicsBody.BodyType.Dynamic, PhysicsCategories.Player,
                PhysicsCategories.Ground | PhysicsCategories.Terrain | PhysicsCategories.Debris,
                gravityScale: 1f);

            _energy = new EnergyModel(config.maxBatteries, config.maxEnergy);
            _freeze = new FreezeController(config.freeze);
            _jumpGesture = new TapHoldGesture(config.jumpAim.holdThreshold);
            _fireGesture = new TapHoldGesture(config.shoot.holdThreshold);
            _shield = new ShieldModule(config.shield);

            var spawner = new PlasmaBulletSpawner();
            _shooting = new ShootingModule(config.shoot, config.shield.reflectSpeedBonus, aimLine, spawner);

            var input = new PlayerInputReader(inputActions);
            _context = new ControllerContext(_body, transform, world, _probe, input, _energy);

            _jumpAimState = new CapsuleJumpAimingState(config.jumpAim, aimLine, _freeze, OnJumpReleased);
            _drillState = new CapsuleDrillState(config.drill, terrain);

            var states = new IControllerState<CapsuleStateId>[]
            {
                new CapsuleGroundState(config.ground, config.boost),
                new CapsuleUnderwaterState(config.underwater, config.boost),
                _jumpAimState,
                new CapsuleShootingState(_shooting, _freeze),
                _drillState
            };

            var initial = _probe.IsSubmerged(transform.position) ? CapsuleStateId.Underwater : CapsuleStateId.Ground;
            _machine = new ControllerStateMachine<CapsuleStateId>(_context, states, initial);
        }

        public void SetInputEnabled(bool isEnabled)
        {
            _inputEnabled = isEnabled;

            // Update()/FixedUpdate() both no-op while disabled, so a freeze started
            // right before possession switches away would otherwise hold
            // Time.timeScale at 0 forever (it affects every agent, not just this one).
            if (!isEnabled)
            {
                _freeze.EndFreeze();
                _shooting.Cancel();
            }
        }

        private void OnJumpReleased()
        {
            var submerged = _probe.IsSubmerged(_body.position);
            _machine.RequestTransition(submerged ? CapsuleStateId.Underwater : CapsuleStateId.Ground);
        }

        private void Update()
        {
            _freeze.Tick();

            if (!_inputEnabled)
                return;

            _context.SampleInput();
            EvaluateTransitions();
            _machine.Tick(Time.unscaledDeltaTime);
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

            // Freeze-driving gestures run on unscaled time so they stay responsive
            // while Time.timeScale is being ramped down or is already at 0.
            _jumpGesture.Tick(input.JumpHeld, input.JumpReleased, Time.unscaledDeltaTime);
            _fireGesture.Tick(input.FireHeld, input.FireReleased, Time.unscaledDeltaTime);

            if (current == CapsuleStateId.Ground || current == CapsuleStateId.Underwater)
            {
                if (_jumpGesture.TapRequested && current == CapsuleStateId.Ground)
                    _body.ApplyLinearImpulseToCenter(Vector2.up * config.jumpAim.hopImpulse, true);

                if (_jumpGesture.HoldBeginRequested)
                {
                    _machine.RequestTransition(CapsuleStateId.JumpAiming);
                    return;
                }

                if (_fireGesture.TapRequested)
                    _shield.Activate(transform);

                if (_fireGesture.HoldBeginRequested)
                {
                    _machine.RequestTransition(CapsuleStateId.Shooting);
                    return;
                }

                var drillDirection = input.Move.sqrMagnitude > 0.01f ? input.Move.normalized : Vector2.down;
                if (input.DrillHeld && _probe.TryGetRockAhead(_body.position, drillDirection, config.drill.probeDistance, out _))
                {
                    _machine.RequestTransition(CapsuleStateId.Drilling);
                    return;
                }

                var submerged = _probe.IsSubmerged(_body.position);
                var desired = submerged ? CapsuleStateId.Underwater : CapsuleStateId.Ground;
                if (desired != current)
                    _machine.RequestTransition(desired);
            }
            else if (current == CapsuleStateId.Shooting && input.FireReleased)
            {
                var submerged = _probe.IsSubmerged(_body.position);
                _machine.RequestTransition(submerged ? CapsuleStateId.Underwater : CapsuleStateId.Ground);
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

        // Nested so it can read the controller's private fields directly rather than
        // forcing ~20 debug-only properties onto CapsuleController's own public API.
        public readonly struct DebugInfo
        {
            private readonly CapsuleController _owner;
            internal DebugInfo(CapsuleController owner) => _owner = owner;

            public bool IsValid => _owner != null && _owner._body.isValid;
            public Vector2 Position => _owner._body.position;
            public Vector2 Velocity => _owner._body.linearVelocity;
            public float BodyRadius => _owner.config.bodyRadius;
            public bool IsGrounded => _owner._probe.IsGrounded(_owner._body.position, _owner.config.bodyRadius);
            public bool IsSubmerged => _owner._probe.IsSubmerged(_owner._body.position);

            public (Vector2 a, Vector2 b, Vector2 c) GroundProbePoints =>
                _owner._probe.GetGroundProbePoints(_owner._body.position, _owner.config.bodyRadius);

            public float FreezeProgress => _owner._freeze.Progress01;

            public bool IsAiming => _owner._machine.CurrentStateId == CapsuleStateId.JumpAiming;
            public Vector2 AimDirection => _owner._jumpAimState.AimDirection;
            public float AimPower => _owner._jumpAimState.AimPower;
            public float MaxAimImpulse => _owner.config.jumpAim.maxImpulse;

            public bool IsShooting => _owner._machine.CurrentStateId == CapsuleStateId.Shooting;
            public bool IsCharging => _owner._shooting.IsCharging;
            public Vector2 ChargePosition => _owner._shooting.ChargePosition;
            public float ChargeRadius => _owner._shooting.ChargeRadius;

            public bool IsDrilling => _owner._machine.CurrentStateId == CapsuleStateId.Drilling;
            public Vector2 DrillDirection => _owner._drillState.DrillDirection;
            public Vector2 DrillProbeTip => _owner._drillState.ProbeTip;
            public float DrillProbeDistance => _owner._drillState.ProbeDistance;
            public float DrillCarveRadius => _owner._drillState.CarveRadius;

            public bool ShieldActive => _owner._shield.IsActive;
            public Vector2 ShieldPosition => _owner._shield.Position;
            public float ShieldRadius => _owner._shield.Radius;

            public bool TryGetRockAhead(out Vector2 hitPoint)
            {
                var move = _owner._context.CurrentInput.Move;
                var direction = IsDrilling ? DrillDirection : move.sqrMagnitude > 0.01f ? move.normalized : Vector2.down;
                return _owner._probe.TryGetRockAhead(_owner._body.position, direction, _owner.config.drill.probeDistance, out hitPoint);
            }
        }
    }
}
