using Game.Gameplay.Controllers.Core;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

namespace Game.Gameplay.Controllers.Capsule
{
    public sealed class CapsuleGroundState : IControllerState<CapsuleStateId>
    {
        public CapsuleStateId Id => CapsuleStateId.Ground;

        private readonly CapsuleConfig.GroundSettings _settings;
        private readonly ShootingModule _shooting;

        public CapsuleGroundState(CapsuleConfig.GroundSettings settings, ShootingModule shooting)
        {
            _settings = settings;
            _shooting = shooting;
        }

        public void Enter(ControllerContext ctx)
        {
            var body = ctx.Body;
            body.type = PhysicsBody.BodyType.Dynamic;
            body.gravityScale = 1f;
            body.linearDamping = 0f;
        }

        public void Exit(ControllerContext ctx) { }

        public void Tick(ControllerContext ctx, float deltaTime) => _shooting?.Tick(ctx, deltaTime);

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime)
        {
            var move = ctx.CurrentInput.Move;
            if (Mathf.Abs(move.x) <= 0.01f)
                return;

            var body = ctx.Body;
            body.ApplyTorque(-move.x * _settings.rollTorque, true);

            var angular = body.angularVelocity;
            if (Mathf.Abs(angular) > _settings.maxAngularSpeed)
                body.angularVelocity = Mathf.Sign(angular) * _settings.maxAngularSpeed;
        }

        public bool CanTransitionTo(CapsuleStateId next, ControllerContext ctx) => true;
    }
}
