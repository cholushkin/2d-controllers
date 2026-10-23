using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // Hold X -> freeze; once the enter ramp fully lands (the de-facto first-shot
    // cooldown), ShootingModule spawns and grows a charge bullet at the muzzle.
    // Release -> ShootingModule launches it and the exit ramp begins.
    public sealed class CapsuleShootingState : IControllerState<CapsuleStateId>
    {
        public CapsuleStateId Id => CapsuleStateId.Shooting;

        private readonly ShootingModule _shooting;
        private readonly FreezeController _freeze;

        public CapsuleShootingState(ShootingModule shooting, FreezeController freeze)
        {
            _shooting = shooting;
            _freeze = freeze;
        }

        public void Enter(ControllerContext ctx) => _freeze.BeginFreeze();

        public void Exit(ControllerContext ctx)
        {
            // A state change away from Shooting always means the fire button released
            // (see CapsuleController.EvaluateTransitions), so release rather than abort.
            _shooting.Release(ctx.CurrentInput.Move);
            _freeze.EndFreeze();
        }

        public void Tick(ControllerContext ctx, float deltaTime)
        {
            var aim = ctx.CurrentInput.Move;
            var direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            var power = Mathf.Clamp01(aim.magnitude);

            _shooting.Tick(ctx.Body.position, direction, power, _freeze.IsFullyFrozen, deltaTime);
        }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime) { }

        public bool CanTransitionTo(CapsuleStateId next, ControllerContext ctx) => true;
    }
}
