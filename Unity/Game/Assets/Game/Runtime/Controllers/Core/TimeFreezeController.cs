using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // Owns Time.timeScale independently of the FSM state lifecycle: the overshoot
    // recovery must keep running for a few frames after JumpAiming has already
    // exited, so it can't live inside that state's Exit().
    public sealed class TimeFreezeController
    {
        private float _overshoot;
        private float _recoverDuration;
        private float _timer;
        private bool _recovering;

        public bool IsFrozen { get; private set; }

        public void Freeze(float timeScale)
        {
            Time.timeScale = timeScale;
            IsFrozen = true;
            _recovering = false;
        }

        public void Release(float overshoot, float recoverDuration)
        {
            IsFrozen = false;
            _overshoot = overshoot;
            _recoverDuration = Mathf.Max(0.01f, recoverDuration);
            _timer = 0f;
            _recovering = overshoot > 1f;
            Time.timeScale = _recovering ? overshoot : 1f;
        }

        public void Tick()
        {
            if (!_recovering)
                return;

            _timer += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(_timer / _recoverDuration);
            Time.timeScale = Mathf.Lerp(_overshoot, 1f, t);

            if (t >= 1f)
                _recovering = false;
        }
    }
}
