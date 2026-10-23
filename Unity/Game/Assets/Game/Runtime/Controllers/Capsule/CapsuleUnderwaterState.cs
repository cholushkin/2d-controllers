using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    public sealed class CapsuleUnderwaterState : IControllerState<CapsuleStateId>
    {
        public CapsuleStateId Id => CapsuleStateId.Underwater;

        private readonly CapsuleConfig.UnderwaterSettings _settings;
        private readonly CapsuleConfig.BoostSettings _boost;

        public CapsuleUnderwaterState(CapsuleConfig.UnderwaterSettings settings, CapsuleConfig.BoostSettings boost)
        {
            _settings = settings;
            _boost = boost;
        }

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
            if (move.sqrMagnitude < 0.0001f)
                return;

            var boosting = ctx.CurrentInput.BoostHeld;
            var drain = _settings.energyDrainPerSecond + (boosting ? _boost.energyDrainPerSecond : 0f);
            if (!ctx.Energy.TryDrainEnergy(drain * fixedDeltaTime))
                return;

            var force = _settings.moveForce * (boosting ? _boost.moveMultiplier : 1f);
            var body = ctx.Body;
            body.linearDamping = _settings.linearDamping * (boosting ? _boost.dampingMultiplier : 1f);
            body.ApplyForceToCenter(move.normalized * force, true);
        }

        public bool CanTransitionTo(CapsuleStateId next, ControllerContext ctx) => true;
    }
}
