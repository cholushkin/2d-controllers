using Game.Gameplay.Controllers.Core;

namespace Game.Gameplay.Controllers.Pilot
{
    public sealed class PilotSwimState : IControllerState<PilotStateId>
    {
        public PilotStateId Id => PilotStateId.Underwater;

        private readonly PilotConfig.SwimSettings _settings;

        public PilotSwimState(PilotConfig.SwimSettings settings) => _settings = settings;

        public void Enter(ControllerContext ctx)
        {
            var body = ctx.Body;
            body.gravityScale = _settings.gravityScale;
            body.linearDamping = _settings.linearDamping;
        }

        public void Exit(ControllerContext ctx)
        {
            var body = ctx.Body;
            body.gravityScale = 1f;
            body.linearDamping = 0f;
        }

        public void Tick(ControllerContext ctx, float deltaTime) { }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime)
        {
            var move = ctx.CurrentInput.Move;
            if (move.sqrMagnitude > 0.0001f)
                ctx.Body.ApplyForceToCenter(move.normalized * _settings.moveForce, true);
        }

        public bool CanTransitionTo(PilotStateId next, ControllerContext ctx) => true;
    }
}
