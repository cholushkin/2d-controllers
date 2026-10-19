using Game.Gameplay.Controllers.Core;

namespace Game.Gameplay.Controllers.Drone
{
    public sealed class DroneEmergencyLandingState : IControllerState<DroneStateId>
    {
        public DroneStateId Id => DroneStateId.EmergencyLanding;

        private readonly DroneConfig.LandingSettings _settings;

        public DroneEmergencyLandingState(DroneConfig.LandingSettings settings) => _settings = settings;

        public void Enter(ControllerContext ctx)
        {
            var body = ctx.Body;
            body.gravityScale = _settings.gravityScale;
            body.linearDamping = 0.5f;
        }

        public void Exit(ControllerContext ctx) { }

        public void Tick(ControllerContext ctx, float deltaTime) { }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime) { }

        public bool CanTransitionTo(DroneStateId next, ControllerContext ctx) => true;
    }
}
