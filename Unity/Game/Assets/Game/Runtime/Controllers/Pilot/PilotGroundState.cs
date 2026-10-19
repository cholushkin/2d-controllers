using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Pilot
{
    public sealed class PilotGroundState : IControllerState<PilotStateId>
    {
        public PilotStateId Id => PilotStateId.Ground;

        private readonly PilotConfig.GroundSettings _settings;

        public PilotGroundState(PilotConfig.GroundSettings settings) => _settings = settings;

        public void Enter(ControllerContext ctx)
        {
            var body = ctx.Body;
            body.gravityScale = 1f;
            body.linearDamping = 0f;
        }

        public void Exit(ControllerContext ctx) { }

        public void Tick(ControllerContext ctx, float deltaTime)
        {
            if (ctx.CurrentInput.JumpPressed)
                ctx.Body.ApplyLinearImpulseToCenter(Vector2.up * _settings.jumpImpulse, true);
        }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime)
        {
            var move = ctx.CurrentInput.Move;
            var body = ctx.Body;
            var velocity = body.linearVelocity;
            velocity.x = move.x * _settings.moveSpeed;
            body.linearVelocity = velocity;
        }

        public bool CanTransitionTo(PilotStateId next, ControllerContext ctx) => true;
    }
}
