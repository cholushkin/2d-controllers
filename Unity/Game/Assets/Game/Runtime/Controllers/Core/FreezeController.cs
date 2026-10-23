using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // Owns Time.timeScale independently of the FSM state lifecycle. Both entering and
    // leaving freeze are authored ramps (curve + duration) rather than instant snaps,
    // so "holding A/X" reads as a deliberate wind-down rather than a hitch.
    public sealed class FreezeController
    {
        private readonly FreezeSettings _settings;

        private enum Phase { Idle, Entering, Frozen, Exiting }
        private Phase _phase = Phase.Idle;
        private float _timer;

        public bool IsFrozen => _phase == Phase.Entering || _phase == Phase.Frozen;

        // True only once the enter ramp has fully reached timeScale 0 - gates effects
        // (e.g. the first charged shot) that must wait for the freeze to "land".
        public bool IsFullyFrozen => _phase == Phase.Frozen;

        // 0 = normal speed, 1 = fully frozen. Drives aim-UI fade-in alongside the ramp.
        public float Progress01 { get; private set; }

        public FreezeController(FreezeSettings settings) => _settings = settings;

        public void BeginFreeze()
        {
            if (_phase == Phase.Entering || _phase == Phase.Frozen)
                return;

            _phase = Phase.Entering;
            _timer = 0f;
        }

        public void EndFreeze()
        {
            if (_phase == Phase.Idle || _phase == Phase.Exiting)
                return;

            _phase = Phase.Exiting;
            _timer = 0f;
        }

        public void Tick()
        {
            switch (_phase)
            {
                case Phase.Entering:
                {
                    var duration = Mathf.Max(0.0001f, _settings.enterDuration);
                    _timer += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(_timer / duration);
                    Progress01 = Mathf.Clamp01(_settings.enterCurve.Evaluate(t));
                    Time.timeScale = 1f - Progress01;

                    if (t >= 1f)
                    {
                        Progress01 = 1f;
                        Time.timeScale = 0f;
                        _phase = Phase.Frozen;
                    }
                    break;
                }
                case Phase.Exiting:
                {
                    var duration = Mathf.Max(0.0001f, _settings.exitDuration);
                    _timer += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(_timer / duration);
                    Progress01 = Mathf.Clamp01(1f - _settings.exitCurve.Evaluate(t));
                    Time.timeScale = 1f - Progress01;

                    if (t >= 1f)
                    {
                        Progress01 = 0f;
                        Time.timeScale = 1f;
                        _phase = Phase.Idle;
                    }
                    break;
                }
            }
        }
    }
}
