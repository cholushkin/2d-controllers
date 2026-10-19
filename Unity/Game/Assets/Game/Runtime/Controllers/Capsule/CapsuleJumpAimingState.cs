using System;
using Game.Gameplay.Controllers.Core;
using Game.Gameplay.Controllers.Director;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // Hold Jump -> time freezes, aim with the stick; release -> time snaps back
    // (with a brief overshoot) and the capsule launches, spending one battery.
    public sealed class CapsuleJumpAimingState : IControllerState<CapsuleStateId>
    {
        public CapsuleStateId Id => CapsuleStateId.JumpAiming;

        private readonly CapsuleConfig.JumpAimSettings _settings;
        private readonly AimLineView _aimLine;
        private readonly TimeFreezeController _timeFreeze;
        private readonly Action _onReleased;

        public CapsuleJumpAimingState(CapsuleConfig.JumpAimSettings settings, AimLineView aimLine,
            TimeFreezeController timeFreeze, Action onReleased)
        {
            _settings = settings;
            _aimLine = aimLine;
            _timeFreeze = timeFreeze;
            _onReleased = onReleased;
        }

        public void Enter(ControllerContext ctx) => _timeFreeze.Freeze(_settings.timeScaleWhileAiming);

        public void Exit(ControllerContext ctx) => _aimLine?.Hide();

        public void Tick(ControllerContext ctx, float deltaTime)
        {
            var aim = ctx.CurrentInput.Aim;
            var power = Mathf.Clamp01(aim.magnitude);

            if (power >= _settings.minPowerThreshold)
                _aimLine?.Show(ctx.Transform.position, aim.normalized, power);
            else
                _aimLine?.Hide();

            if (ctx.CurrentInput.JumpReleased)
                Release(ctx, aim, power);
        }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime) { }

        private void Release(ControllerContext ctx, Vector2 aim, float power)
        {
            _timeFreeze.Release(_settings.releaseOvershoot, _settings.timeScaleRecoverDuration);

            if (power >= _settings.minPowerThreshold && ctx.Energy.TrySpendBattery())
                ctx.Body.ApplyLinearImpulseToCenter(aim.normalized * (power * _settings.maxImpulse), true);

            _onReleased?.Invoke();
        }

        public bool CanTransitionTo(CapsuleStateId next, ControllerContext ctx) => true;
    }
}
