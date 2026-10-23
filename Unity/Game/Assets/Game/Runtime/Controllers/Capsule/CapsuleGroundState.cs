using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    public sealed class CapsuleGroundState : IControllerState<CapsuleStateId>
    {
        public CapsuleStateId Id => CapsuleStateId.Ground;

        private readonly CapsuleConfig.GroundSettings _settings;
        private readonly CapsuleConfig.BoostSettings _boost;

        public CapsuleGroundState(CapsuleConfig.GroundSettings settings, CapsuleConfig.BoostSettings boost)
        {
            _settings = settings;
            _boost = boost;
        }

        public void Enter(ControllerContext ctx)
        {
            var body = ctx.Body;
            body.type = Unity.U2D.Physics.PhysicsBody.BodyType.Dynamic;
            body.gravityScale = 1f;
            body.linearDamping = 0f;
        }

        public void Exit(ControllerContext ctx) { }

        public void Tick(ControllerContext ctx, float deltaTime) { }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime)
        {
            var move = ctx.CurrentInput.Move;
            if (Mathf.Abs(move.x) <= 0.01f)
                return;

            var boosting = ctx.CurrentInput.BoostHeld;
            var torque = _settings.rollTorque * (boosting ? _boost.moveMultiplier : 1f);

            var body = ctx.Body;
            body.ApplyTorque(-move.x * torque, true);

            var angular = body.angularVelocity;
            if (Mathf.Abs(angular) > _settings.maxAngularSpeed)
                body.angularVelocity = Mathf.Sign(angular) * _settings.maxAngularSpeed;
        }

        public bool CanTransitionTo(CapsuleStateId next, ControllerContext ctx) => true;
    }
}
