using Game.Gameplay.Controllers.Core;
using Game.Gameplay.Controllers.Director;

namespace Game.Gameplay.Controllers.Capsule
{
    // Overlay usable from Ground/Underwater - not a full FSM state, since shooting
    // doesn't block rolling or swimming.
    public sealed class ShootingModule
    {
        private readonly CapsuleConfig.ShootSettings _settings;
        private readonly AimLineView _aimLine;
        private readonly PlasmaBulletSpawner _spawner;

        private float _cooldown;

        public ShootingModule(CapsuleConfig.ShootSettings settings, AimLineView aimLine, PlasmaBulletSpawner spawner)
        {
            _settings = settings;
            _aimLine = aimLine;
            _spawner = spawner;
        }

        public void Tick(ControllerContext ctx, float deltaTime)
        {
            var input = ctx.CurrentInput;
            _cooldown -= deltaTime;

            if (!input.AttackHeld || input.Aim.sqrMagnitude < 0.0001f)
            {
                _aimLine?.Hide();
                return;
            }

            var direction = input.Aim.normalized;
            _aimLine?.Show(ctx.Transform.position, direction, 1f);

            if (_cooldown > 0f)
                return;

            _cooldown = _settings.fireInterval;
            _spawner.Spawn(ctx.Body.position, direction, _settings);
        }
    }
}
