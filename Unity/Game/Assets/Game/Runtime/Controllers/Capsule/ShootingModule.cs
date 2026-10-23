using System.Collections.Generic;
using Game.Gameplay.Controllers.Core;
using Game.Gameplay.Controllers.Director;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // Owns every bullet charging at the muzzle while X is held: once the owner is
    // fully frozen (that freeze ramp is the de-facto first-shot cooldown), spawns a
    // fresh bullet every fireInterval - or, with mergeNearbyBullets on, grows the
    // nearest pending one instead of adding a separate bullet. Release launches
    // everything still pending; the exit ramp then unfreezes.
    public sealed class ShootingModule
    {
        private readonly CapsuleConfig.ShootSettings _settings;
        private readonly float _reflectSpeedBonus;
        private readonly AimLineView _aimLine;
        private readonly PlasmaBulletSpawner _spawner;
        private readonly List<PlasmaBullet> _pending = new();

        private float _timer;

        public bool IsCharging => _pending.Count > 0;
        public IReadOnlyList<PlasmaBullet> PendingBullets => _pending;

        public ShootingModule(CapsuleConfig.ShootSettings settings, float reflectSpeedBonus, AimLineView aimLine, PlasmaBulletSpawner spawner)
        {
            _settings = settings;
            _reflectSpeedBonus = reflectSpeedBonus;
            _aimLine = aimLine;
            _spawner = spawner;
        }

        public void Tick(Vector2 muzzlePosition, Vector2 aimDirection, float aimPower, bool fullyFrozen, float unscaledDeltaTime)
        {
            if (fullyFrozen)
            {
                _timer += unscaledDeltaTime;
                if (_pending.Count == 0 || _timer >= _settings.fireInterval)
                {
                    _timer = 0f;
                    SpawnOrMerge(muzzlePosition);
                }
            }

            if (aimPower >= 0.0001f)
                _aimLine?.Show(muzzlePosition, aimDirection, aimPower);
            else
                _aimLine?.Hide();
        }

        private void SpawnOrMerge(Vector2 position)
        {
            if (_settings.mergeNearbyBullets)
            {
                PlasmaBullet nearest = null;
                var nearestDistanceSq = float.MaxValue;
                foreach (var candidate in _pending)
                {
                    var distanceSq = (candidate.Position - position).sqrMagnitude;
                    if (distanceSq <= _settings.mergeRadius * _settings.mergeRadius && distanceSq < nearestDistanceSq)
                    {
                        nearest = candidate;
                        nearestDistanceSq = distanceSq;
                    }
                }

                if (nearest != null)
                {
                    nearest.Grow(_settings.radiusGrowth, _settings.speedGrowth);
                    return;
                }
            }

            if (_pending.Count >= _settings.maxActiveBullets)
                return;

            var bullet = _spawner.Spawn(position, _settings.baseRadius, _settings.baseSpeed, _reflectSpeedBonus, _settings.bulletLifetime);
            _pending.Add(bullet);
        }

        public void Release(Vector2 aimDirection)
        {
            _aimLine?.Hide();
            if (_pending.Count == 0)
                return;

            var direction = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
            foreach (var bullet in _pending)
                bullet.Launch(direction);

            _pending.Clear();
        }

        public void Cancel()
        {
            _aimLine?.Hide();
            foreach (var bullet in _pending)
                bullet.Abort();

            _pending.Clear();
        }
    }
}
