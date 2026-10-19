using Game.Gameplay.Controllers.Core;

namespace Game.Gameplay.Controllers.Drone
{
    public sealed class DroneFlyingState : IControllerState<DroneStateId>
    {
        public DroneStateId Id => DroneStateId.Flying;

        private readonly DroneConfig.FlightSettings _settings;

        public DroneFlyingState(DroneConfig.FlightSettings settings) => _settings = settings;

        public void Enter(ControllerContext ctx)
        {
            var body = ctx.Body;
            body.gravityScale = 0f;
            body.linearDamping = _settings.linearDamping;
        }

        public void Exit(ControllerContext ctx) { }

        public void Tick(ControllerContext ctx, float deltaTime) { }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime)
        {
            var move = ctx.CurrentInput.Move;
            if (move.sqrMagnitude <= 0.0001f)
                return;

            if (!ctx.Energy.TryDrainEnergy(_settings.energyDrainPerSecond * fixedDeltaTime))
                return;

            var body = ctx.Body;
            body.ApplyForceToCenter(move.normalized * _settings.thrustForce, true);

            var velocity = body.linearVelocity;
            if (velocity.magnitude > _settings.maxSpeed)
                body.linearVelocity = velocity.normalized * _settings.maxSpeed;
        }

        public bool CanTransitionTo(DroneStateId next, ControllerContext ctx) => true;
    }
}
