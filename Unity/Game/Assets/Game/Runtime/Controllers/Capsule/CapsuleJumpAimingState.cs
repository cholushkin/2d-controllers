using System;
using Game.Gameplay.Controllers.Core;
using Game.Gameplay.Controllers.Director;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // Hold A -> freeze (enter ramp), aim with the left stick; release -> jump (spends
    // one battery) and begin the exit ramp back to full speed.
    public sealed class CapsuleJumpAimingState : IControllerState<CapsuleStateId>
    {
        public CapsuleStateId Id => CapsuleStateId.JumpAiming;

        private readonly CapsuleConfig.JumpAimSettings _settings;
        private readonly AimLineView _aimLine;
        private readonly FreezeController _freeze;
        private readonly Action _onReleased;

        public Vector2 AimDirection { get; private set; }
        public float AimPower { get; private set; }

        public CapsuleJumpAimingState(CapsuleConfig.JumpAimSettings settings, AimLineView aimLine,
            FreezeController freeze, Action onReleased)
        {
            _settings = settings;
            _aimLine = aimLine;
            _freeze = freeze;
            _onReleased = onReleased;
        }

        public void Enter(ControllerContext ctx) => _freeze.BeginFreeze();

        public void Exit(ControllerContext ctx) => _aimLine?.Hide();

        public void Tick(ControllerContext ctx, float deltaTime)
        {
            var aim = ctx.CurrentInput.Move;
            var power = Mathf.Clamp01(aim.magnitude);
            AimDirection = power > 0.0001f ? aim.normalized : Vector2.zero;
            AimPower = power;

            if (power >= _settings.minPowerThreshold)
                _aimLine?.Show(ctx.Transform.position, AimDirection, power);
            else
                _aimLine?.Hide();

            if (ctx.CurrentInput.JumpReleased)
                Release(ctx, aim, power);
        }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime) { }

        private void Release(ControllerContext ctx, Vector2 aim, float power)
        {
            _freeze.EndFreeze();

            if (power >= _settings.minPowerThreshold && ctx.Energy.TrySpendBattery())
                ctx.Body.ApplyLinearImpulseToCenter(aim.normalized * (power * _settings.maxImpulse), true);

            _onReleased?.Invoke();
        }

        public bool CanTransitionTo(CapsuleStateId next, ControllerContext ctx) => true;
    }
}
