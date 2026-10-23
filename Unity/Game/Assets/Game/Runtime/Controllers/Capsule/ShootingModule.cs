using Game.Gameplay.Controllers.Core;
using Game.Gameplay.Controllers.Director;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // Owns the single "charge bullet" grown while X is held: spawns it once the owner
    // is fully frozen (that freeze ramp is the de-facto first-shot cooldown), grows its
    // radius/speed every fireInterval while still held, and launches it on release.
    public sealed class ShootingModule
    {
        private readonly CapsuleConfig.ShootSettings _settings;
        private readonly float _reflectSpeedBonus;
        private readonly AimLineView _aimLine;
        private readonly PlasmaBulletSpawner _spawner;

        private PlasmaBullet _chargeBullet;
        private int _chargeSteps;
        private float _timer;

        public bool IsCharging => _chargeBullet != null;
        public Vector2 ChargePosition => _chargeBullet != null ? _chargeBullet.Position : default;
        public float ChargeRadius => _settings.baseRadius + _chargeSteps * _settings.radiusGrowth;

        public ShootingModule(CapsuleConfig.ShootSettings settings, float reflectSpeedBonus, AimLineView aimLine, PlasmaBulletSpawner spawner)
        {
            _settings = settings;
            _reflectSpeedBonus = reflectSpeedBonus;
            _aimLine = aimLine;
            _spawner = spawner;
        }

        public void Tick(Vector2 muzzlePosition, Vector2 aimDirection, float aimPower, bool fullyFrozen, float unscaledDeltaTime)
        {
            if (_chargeBullet == null)
            {
                if (!fullyFrozen)
                    return;

                _chargeBullet = _spawner.Spawn(muzzlePosition, _settings.baseRadius, _reflectSpeedBonus, _settings.bulletLifetime);
                _chargeSteps = 0;
                _timer = 0f;
            }
            else
            {
                _timer += unscaledDeltaTime;
                if (_timer >= _settings.fireInterval && _chargeSteps < _settings.maxChargeSteps)
                {
                    _timer = 0f;
                    _chargeSteps++;
                    _chargeBullet.SetRadius(_settings.baseRadius + _chargeSteps * _settings.radiusGrowth);
                }
            }

            if (aimPower >= 0.0001f)
                _aimLine?.Show(muzzlePosition, aimDirection, aimPower);
            else
                _aimLine?.Hide();
        }

        public void Release(Vector2 aimDirection)
        {
            _aimLine?.Hide();
            if (_chargeBullet == null)
                return;

            var speed = _settings.baseSpeed + _chargeSteps * _settings.speedGrowth;
            var direction = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
            _chargeBullet.Launch(direction * speed);
            _chargeBullet = null;
        }

        public void Cancel()
        {
            _aimLine?.Hide();
            if (_chargeBullet == null)
                return;

            _chargeBullet.Abort();
            _chargeBullet = null;
        }
    }
}
