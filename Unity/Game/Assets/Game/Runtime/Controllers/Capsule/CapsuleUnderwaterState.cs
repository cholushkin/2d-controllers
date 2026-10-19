using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    public sealed class CapsuleUnderwaterState : IControllerState<CapsuleStateId>
    {
        public CapsuleStateId Id => CapsuleStateId.Underwater;

        private readonly CapsuleConfig.UnderwaterSettings _settings;
        private readonly ShootingModule _shooting;

        public CapsuleUnderwaterState(CapsuleConfig.UnderwaterSettings settings, ShootingModule shooting)
        {
            _settings = settings;
            _shooting = shooting;
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

        public void Tick(ControllerContext ctx, float deltaTime) => _shooting?.Tick(ctx, deltaTime);

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime)
        {
            var move = ctx.CurrentInput.Move;
            if (move.sqrMagnitude < 0.0001f)
                return;

            if (!ctx.Energy.TryDrainEnergy(_settings.energyDrainPerSecond * fixedDeltaTime))
                return;

            ctx.Body.ApplyForceToCenter(move.normalized * _settings.moveForce, true);
        }

        public bool CanTransitionTo(CapsuleStateId next, ControllerContext ctx) => true;
    }
}
